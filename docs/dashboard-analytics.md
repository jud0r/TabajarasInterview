# Recruitment Dashboard — Analytics Reference

This document defines the contract and analytics behind the recruitment dashboard
(`/dashboard`). The Blazor frontend calls a **single aggregated endpoint** and renders
the result; all heavy lifting (counting, averaging, bucketing) happens in SQL on the API
side for performance.

- Frontend page: [`Dashboard.razor`](../TabajarasInterview.Web/Components/Pages/Dashboard.razor)
- API service: [`DashboardApiService`](../TabajarasInterview.Web/Services/Api/DashboardApiService.cs)
- Response model: [`DashboardData`](../TabajarasInterview.Web/DTOs/Dashboard/DashboardData.cs)
- Request filter: [`DashboardFilter`](../TabajarasInterview.Web/DTOs/Dashboard/DashboardFilter.cs)

While the endpoint is not yet implemented, the page falls back to
[`DashboardSampleData`](../TabajarasInterview.Web/Services/Api/DashboardSampleData.cs) and
shows an info banner so it stays demonstrable.

---

## 1. Endpoint contract

```
GET /api/dashboard/overview
Authorization: Bearer <jwt>            # delivered via the tabajaras_access_token cookie
```

### Query parameters (all optional)

| Param            | Type   | Example       | Applies to |
|------------------|--------|---------------|------------|
| `from`           | date   | `2025-01-01`  | application / interview date range (inclusive) |
| `to`             | date   | `2025-03-31`  | application / interview date range (inclusive) |
| `position_id`    | int    | `3`           | restrict every section to one position |
| `interview_type` | string | `Technical`   | restrict interview analytics to one type |

### Response shape

A single JSON object matching `DashboardData`. Top-level members:

`kpis`, `funnel`, `statusDistribution`, `interviewTypes`, `averageInterviewsPerCandidate`,
`interviewSuccessRate`, `reviewers`, `topCandidates`, `recentCandidates`,
`candidatesPerPosition`, `positions`, `demandingStacks`, `questions`, `timeMetrics`,
`hiringOverTime`, `alerts`.

> **Why one endpoint?** Fanning out to `/candidates`, `/applications`, `/interviews`,
> `/positions`, `/reviewers` would mean 5+ round-trips and client-side aggregation over
> potentially large result sets. A purpose-built overview endpoint returns a small, fixed
> payload regardless of data volume.

---

## 2. Performance guidance

The aggregations below assume these indexes exist (add the ones you're missing):

```sql
CREATE INDEX idx_app_position      ON candidate_applications (position_id);
CREATE INDEX idx_app_status        ON candidate_applications (status);
CREATE INDEX idx_app_created       ON candidate_applications (created_at);
CREATE INDEX idx_app_finished      ON candidate_applications (started_at, finished_at);
CREATE INDEX idx_interview_app     ON interviews (application_id);
CREATE INDEX idx_interview_type    ON interviews (type);
CREATE INDEX idx_ir_interview      ON interview_reviewers (interview_id);
CREATE INDEX idx_ir_user           ON interview_reviewers (user_id);
CREATE INDEX idx_iq_interview      ON interview_questions (interview_id);
CREATE INDEX idx_iq_question       ON interview_questions (question_id);
CREATE INDEX idx_ps_position       ON position_stacks (position_id);
```

General rules:
- Compute each metric as an **aggregate query**, not by pulling rows into the app.
- Apply the `from`/`to`/`position_id`/`interview_type` filters inside every query (shown as
  `:from`, `:to`, `:position_id`, `:type` placeholders below — bind them as parameters).
- For dashboards refreshed often on large datasets, consider a **summary/materialized table**
  refreshed on a schedule, or cache the overview response per filter for ~1 minute.

> The `status` string values used below (`'applied'`, `'in_interview'`, `'offer'`,
> `'approved'`, `'hired'`, `'rejected'`) are illustrative — map them to your actual
> `candidate_applications.status` domain.

---

## 3. KPIs (`kpis`)

**Meaning:** headline counters for the top cards. Pass rate = approved/hired share of
completed applications.

```sql
SELECT
  (SELECT COUNT(*) FROM candidates)                                          AS total_candidates,
  (SELECT COUNT(*) FROM positions WHERE is_open = 1)                         AS active_positions,
  COUNT(*)                                                                   AS total_applications,
  SUM(ca.status IN ('applied','in_interview','offer'))                       AS applications_in_progress,
  SUM(ca.status IN ('approved','hired','rejected'))                          AS completed_applications,
  ROUND(AVG(ca.final_score), 2)                                             AS average_final_score,
  ROUND(
	100 * SUM(ca.status IN ('approved','hired'))
		/ NULLIF(SUM(ca.status IN ('approved','hired','rejected')), 0), 1)   AS pass_rate
FROM candidate_applications ca
WHERE (:from IS NULL OR ca.created_at >= :from)
  AND (:to   IS NULL OR ca.created_at <  :to + INTERVAL 1 DAY)
  AND (:position_id IS NULL OR ca.position_id = :position_id);

-- Average interview score (kpis.average_interview_score)
SELECT ROUND(AVG(i.score), 2) AS average_interview_score
FROM interviews i
JOIN candidate_applications ca ON ca.id = i.application_id
WHERE (:from IS NULL OR i.created_at >= :from)
  AND (:to   IS NULL OR i.created_at <  :to + INTERVAL 1 DAY)
  AND (:position_id IS NULL OR ca.position_id = :position_id)
  AND (:type IS NULL OR i.type = :type);
```

---

## 4. Applications funnel (`funnel`) & status distribution (`statusDistribution`)

**Meaning:** the funnel collapses statuses into ordered pipeline stages; the frontend
computes stage-to-stage conversion. The pie/donut uses the raw status breakdown.

```sql
-- Funnel stages (order + IsTerminal are assigned by the API)
SELECT
  SUM(ca.status = 'applied')                                  AS applied,
  SUM(ca.status IN ('in_interview'))                          AS in_interview,
  SUM(ca.status IN ('offer','approved','hired'))              AS offer_approved,
  SUM(ca.status = 'rejected')                                 AS rejected      -- terminal/loss
FROM candidate_applications ca
WHERE (:position_id IS NULL OR ca.position_id = :position_id)
  AND (:from IS NULL OR ca.created_at >= :from)
  AND (:to   IS NULL OR ca.created_at <  :to + INTERVAL 1 DAY);

-- Status distribution (one slice per raw status)
SELECT ca.status AS status, COUNT(*) AS count
FROM candidate_applications ca
WHERE (:position_id IS NULL OR ca.position_id = :position_id)
  AND (:from IS NULL OR ca.created_at >= :from)
  AND (:to   IS NULL OR ca.created_at <  :to + INTERVAL 1 DAY)
GROUP BY ca.status;
```

> Conversion between stages is `stage[n].count / stage[n-1].count` and is calculated in
> [`ApplicationsFunnel.razor`](../TabajarasInterview.Web/Components/Shared/Dashboard/ApplicationsFunnel.razor),
> so the API only needs to return counts.

---

## 5. Interview analytics (`interviewTypes`, `averageInterviewsPerCandidate`, `interviewSuccessRate`)

**Meaning:** per-type volume, quality (avg score) and success rate; plus how many
interviews a candidate goes through on average and the overall interview success rate.

```sql
-- Per interview type
SELECT
  i.type                                              AS type,
  COUNT(*)                                            AS count,
  ROUND(AVG(i.score), 2)                              AS average_score,
  ROUND(100 * SUM(i.score >= 6) / NULLIF(COUNT(*),0), 1) AS success_rate   -- 6 = pass threshold
FROM interviews i
JOIN candidate_applications ca ON ca.id = i.application_id
WHERE (:position_id IS NULL OR ca.position_id = :position_id)
  AND (:from IS NULL OR i.created_at >= :from)
  AND (:to   IS NULL OR i.created_at <  :to + INTERVAL 1 DAY)
GROUP BY i.type;

-- Average interviews per candidate
SELECT ROUND(COUNT(*) / NULLIF(COUNT(DISTINCT ca.candidate_id), 0), 2) AS avg_per_candidate
FROM interviews i
JOIN candidate_applications ca ON ca.id = i.application_id
WHERE (:position_id IS NULL OR ca.position_id = :position_id);

-- Overall interview success rate
SELECT ROUND(100 * SUM(i.score >= 6) / NULLIF(COUNT(*), 0), 1) AS success_rate
FROM interviews i
JOIN candidate_applications ca ON ca.id = i.application_id
WHERE (:position_id IS NULL OR ca.position_id = :position_id)
  AND (:type IS NULL OR i.type = :type);
```

---

## 6. Reviewer performance & bias (`reviewers`)

**Meaning:** for each reviewer (from `interview_reviewers`), how many interviews they
scored, their average score, and `biasDelta = reviewer_avg − global_avg`. A large absolute
delta flags a potentially harsh/lenient reviewer. The UI marks `|delta| ≥ 1.0` as
"Possible bias".

```sql
SELECT
  u.name                                          AS reviewer,
  COUNT(*)                                         AS interviews_conducted,
  ROUND(AVG(ir.score), 2)                          AS average_score,
  ROUND(AVG(ir.score) - g.global_avg, 2)           AS bias_delta
FROM interview_reviewers ir
JOIN users u ON u.id = ir.user_id
JOIN interviews i ON i.id = ir.interview_id
JOIN candidate_applications ca ON ca.id = i.application_id
CROSS JOIN (SELECT AVG(score) AS global_avg FROM interview_reviewers) g
WHERE (:position_id IS NULL OR ca.position_id = :position_id)
GROUP BY u.id, u.name, g.global_avg
ORDER BY average_score DESC;
```

> **Statistical note:** `biasDelta` is a simple mean comparison. For production-grade bias
> detection, prefer the reviewer's deviation **relative to the consensus score of the same
> interviews** (controls for which candidates each reviewer happened to see), and report
> variance/standard deviation alongside the mean.

---

## 7. Candidates insights (`topCandidates`, `recentCandidates`, `candidatesPerPosition`)

```sql
-- Top candidates by final score
SELECT c.id, c.name, p.title AS position, ca.final_score, ca.status
FROM candidate_applications ca
JOIN candidates c ON c.id = ca.candidate_id
JOIN positions  p ON p.id = ca.position_id
WHERE ca.final_score IS NOT NULL
  AND (:position_id IS NULL OR ca.position_id = :position_id)
ORDER BY ca.final_score DESC
LIMIT 10;

-- Recently added candidates
SELECT c.id, c.name, p.title AS position, c.created_at
FROM candidates c
LEFT JOIN candidate_applications ca ON ca.candidate_id = c.id
LEFT JOIN positions p ON p.id = ca.position_id
ORDER BY c.created_at DESC
LIMIT 10;

-- Candidates per position
SELECT p.title AS label, COUNT(DISTINCT ca.candidate_id) AS count
FROM candidate_applications ca
JOIN positions p ON p.id = ca.position_id
GROUP BY p.id, p.title
ORDER BY count DESC;
```

---

## 8. Positions overview (`positions`, `demandingStacks`)

```sql
-- Per-position metrics + its most common stack
SELECT
  p.id,
  p.title           AS position,
  p.is_open         AS is_open,
  COUNT(ca.id)      AS applications,
  ROUND(AVG(ca.final_score), 2) AS average_score,
  (SELECT s.name
	 FROM position_stacks ps
	 JOIN stacks s ON s.id = ps.stack_id
	WHERE ps.position_id = p.id
	ORDER BY ps.weight DESC LIMIT 1) AS top_stack
FROM positions p
LEFT JOIN candidate_applications ca ON ca.position_id = p.id
GROUP BY p.id, p.title, p.is_open;

-- Most demanding stacks (open positions + applications requiring the stack)
SELECT
  s.name                                  AS stack,
  COUNT(DISTINCT CASE WHEN p.is_open = 1 THEN p.id END) AS open_positions,
  COUNT(ca.id)                            AS applications
FROM position_stacks ps
JOIN stacks s    ON s.id = ps.stack_id
JOIN positions p ON p.id = ps.position_id
LEFT JOIN candidate_applications ca ON ca.position_id = p.id
GROUP BY s.id, s.name
ORDER BY applications DESC
LIMIT 10;
```

---

## 9. Questions / evaluation (`questions`)

**Meaning:** how often a question is used and its average score. A low average flags a
**difficult** question (the UI marks `0 < avg < 5` as "Hard").

```sql
SELECT
  q.text                  AS question,
  COUNT(*)                AS times_used,
  ROUND(AVG(iq.score), 2) AS average_score
FROM interview_questions iq
JOIN questions q  ON q.id = iq.question_id
JOIN interviews i ON i.id = iq.interview_id
JOIN candidate_applications ca ON ca.id = i.application_id
WHERE (:position_id IS NULL OR ca.position_id = :position_id)
  AND (:type IS NULL OR i.type = :type)
GROUP BY q.id, q.text
ORDER BY times_used DESC
LIMIT 25;
```

---

## 10. Time metrics (`timeMetrics`, `hiringOverTime`)

**Meaning:** average end-to-end hiring duration, interview turnaround, average time per
stage, and an applications-vs-hires time series for the trend line chart.

```sql
-- Average hiring time (days) for completed applications
SELECT ROUND(AVG(TIMESTAMPDIFF(DAY, ca.started_at, ca.finished_at)), 1) AS avg_hiring_days
FROM candidate_applications ca
WHERE ca.finished_at IS NOT NULL
  AND (:position_id IS NULL OR ca.position_id = :position_id);

-- Interview turnaround: scheduled -> reviewed (hours)
SELECT ROUND(AVG(TIMESTAMPDIFF(HOUR, i.scheduled_at, i.reviewed_at)), 1) AS turnaround_hours
FROM interviews i
JOIN candidate_applications ca ON ca.id = i.application_id
WHERE i.reviewed_at IS NOT NULL
  AND (:position_id IS NULL OR ca.position_id = :position_id);

-- Hiring over time (monthly applications vs hires)
SELECT
  DATE_FORMAT(ca.created_at, '%Y-%m')                       AS period,
  COUNT(*)                                                  AS applications,
  SUM(ca.status IN ('approved','hired'))                    AS hires
FROM candidate_applications ca
WHERE (:from IS NULL OR ca.created_at >= :from)
  AND (:to   IS NULL OR ca.created_at <  :to + INTERVAL 1 DAY)
  AND (:position_id IS NULL OR ca.position_id = :position_id)
GROUP BY period
ORDER BY period;
```

> `stageDurations` (avg days per pipeline stage) requires a stage-transition history table
> (e.g. `application_status_history(application_id, status, changed_at)`). Compute the time
> between consecutive transitions per application, then average per stage. If you don't track
> transitions yet, omit `stageDurations` and the UI section will simply render empty.

---

## 11. Alerts (`alerts`)

**Meaning:** actionable, severity-tagged messages surfaced at the top of the page
(`info` / `success` / `warning` / `error`). Generate them from thresholds, e.g.:

- **Low-score candidates** — active candidates whose average interview score `< 4.0`.
- **Slow hiring process** — positions whose average open-to-close time exceeds the target
  (e.g. 25 days).
- **Reviewer bias** — any reviewer with `|biasDelta| ≥ 1.5`.

```sql
-- Example: count active low-score candidates for the "Low-score" alert
SELECT COUNT(*) AS low_score_active
FROM (
  SELECT ca.candidate_id, AVG(i.score) AS avg_score
  FROM candidate_applications ca
  JOIN interviews i ON i.application_id = ca.id
  WHERE ca.status IN ('applied','in_interview','offer')
  GROUP BY ca.candidate_id
  HAVING AVG(i.score) < 4.0
) t;
```

---

## 12. Metric glossary

| Metric | Definition |
|--------|------------|
| **Pass rate** | `approved+hired` ÷ `approved+hired+rejected`, as a percentage of *completed* applications. |
| **Conversion** | Count at a stage ÷ count at the previous progressing stage. |
| **Success rate (interview)** | Share of interviews scoring at/above the pass threshold (default 6/10). |
| **Avg interviews / candidate** | Total interviews ÷ distinct candidates. |
| **Bias Δ** | Reviewer average score − global average score. `|Δ| ≥ 1.0` flagged in UI. |
| **Difficult question** | A question with average score `0 < avg < 5`. |
| **Avg hiring time** | Mean days from `started_at` to `finished_at` for completed applications. |
| **Interview turnaround** | Mean hours from an interview being scheduled to being reviewed. |

---

## 13. Filters & drill-down behavior (frontend)

- The filter bar (date range, position, interview type) re-requests the overview endpoint via
  `DashboardFilter.ToQueryString()`; clearing resets to the all-time/all-positions view.
- Clicking the **Total candidates**, **Total applications**, or **Avg final score** KPI cards
  toggles an inline drill-down panel that lists the supporting rows (recent candidates,
  per-stage counts, or top candidates respectively).
- Tables (top candidates, positions, questions, reviewers) are client-side **sortable**, and
  the first three are **filterable** via their search box.
