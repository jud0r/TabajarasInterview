namespace TabajarasInterview.Web.DTOs.Dashboard;

/// <summary>
/// Aggregated payload backing the recruitment dashboard. A single round-trip keeps
/// the UI fast: the API computes every metric with SQL aggregations (see
/// <c>docs/dashboard-analytics.md</c>) instead of the client fanning out to
/// <c>/candidates</c>, <c>/applications</c>, <c>/interviews</c>, etc.
/// </summary>
public sealed class DashboardData
{
    public DashboardKpis Kpis { get; set; } = new();

    /// <summary>Ordered application pipeline stages (Applied → In Interview → Offer → Rejected).</summary>
    public List<FunnelStage> Funnel { get; set; } = [];

    /// <summary>Status distribution for the pie/donut chart.</summary>
    public List<StatusSlice> StatusDistribution { get; set; } = [];

    /// <summary>Per interview-type counts, average score and success rate.</summary>
    public List<InterviewTypeStat> InterviewTypes { get; set; } = [];

    public double AverageInterviewsPerCandidate { get; set; }

    public double InterviewSuccessRate { get; set; }

    /// <summary>Reviewer performance + bias detection (delta vs. global average).</summary>
    public List<ReviewerStat> Reviewers { get; set; } = [];

    public List<TopCandidate> TopCandidates { get; set; } = [];

    public List<RecentCandidate> RecentCandidates { get; set; } = [];

    public List<NamedCount> CandidatesPerPosition { get; set; } = [];

    public List<PositionStat> Positions { get; set; } = [];

    public List<StackDemand> DemandingStacks { get; set; } = [];

    public List<QuestionStat> Questions { get; set; } = [];

    public TimeMetrics TimeMetrics { get; set; } = new();

    /// <summary>Applications vs. hires per period for the "hiring over time" line chart.</summary>
    public List<TimePoint> HiringOverTime { get; set; } = [];

    public List<DashboardAlert> Alerts { get; set; } = [];
}

/// <summary>High-level KPI counters rendered as the top cards.</summary>
public sealed class DashboardKpis
{
    public int TotalCandidates { get; set; }
    public int ActivePositions { get; set; }
    public int TotalApplications { get; set; }
    public int ApplicationsInProgress { get; set; }
    public int CompletedApplications { get; set; }

    /// <summary>Average of <c>candidate_applications.final_score</c>.</summary>
    public double AverageFinalScore { get; set; }

    /// <summary>Average of <c>interviews.score</c>.</summary>
    public double AverageInterviewScore { get; set; }

    /// <summary>Percentage (0-100) of finished applications that resulted in an offer/approval.</summary>
    public double PassRate { get; set; }
}

/// <summary>A single stage of the applications funnel.</summary>
public sealed class FunnelStage
{
    public string Stage { get; set; } = string.Empty;
    public int Count { get; set; }

    /// <summary>True for terminal "loss" stages (e.g. Rejected) so the UI can style them differently.</summary>
    public bool IsTerminal { get; set; }
}

/// <summary>Status slice for the distribution pie/donut chart.</summary>
public sealed class StatusSlice
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>Aggregated metrics for one interview type (HR, Technical, ...).</summary>
public sealed class InterviewTypeStat
{
    public string Type { get; set; } = string.Empty;
    public int Count { get; set; }
    public double AverageScore { get; set; }

    /// <summary>Percentage (0-100) of interviews of this type with a passing outcome.</summary>
    public double SuccessRate { get; set; }
}

/// <summary>Reviewer performance derived from <c>interview_reviewers</c>.</summary>
public sealed class ReviewerStat
{
    public string Reviewer { get; set; } = string.Empty;
    public int InterviewsConducted { get; set; }
    public double AverageScore { get; set; }

    /// <summary>Reviewer average minus the global average. Large |delta| hints at scoring bias.</summary>
    public double BiasDelta { get; set; }
}

/// <summary>Candidate ranked by final score.</summary>
public sealed class TopCandidate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public double FinalScore { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>Recently added candidate.</summary>
public sealed class RecentCandidate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>Generic name/count pair used by simple bar charts and tables.</summary>
public sealed class NamedCount
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>Per-position overview metrics.</summary>
public sealed class PositionStat
{
    public int Id { get; set; }
    public string Position { get; set; } = string.Empty;
    public bool IsOpen { get; set; }
    public int Applications { get; set; }
    public double AverageScore { get; set; }
    public string TopStack { get; set; } = string.Empty;
}

/// <summary>Stack demand derived from <c>position_stacks</c>.</summary>
public sealed class StackDemand
{
    public string Stack { get; set; } = string.Empty;
    public int OpenPositions { get; set; }
    public int Applications { get; set; }
}

/// <summary>Question usage and difficulty derived from <c>interview_questions</c>.</summary>
public sealed class QuestionStat
{
    public string Question { get; set; } = string.Empty;
    public int TimesUsed { get; set; }
    public double AverageScore { get; set; }

    /// <summary>Convenience flag: a low average score marks the question as "hard".</summary>
    public bool IsDifficult => AverageScore is > 0 and < 5;
}

/// <summary>Time-based hiring metrics.</summary>
public sealed class TimeMetrics
{
    /// <summary>Average days between <c>started_at</c> and <c>finished_at</c> for completed applications.</summary>
    public double AverageHiringDays { get; set; }

    /// <summary>Average hours between an interview being scheduled and reviewed.</summary>
    public double InterviewTurnaroundHours { get; set; }

    public List<StageDuration> StageDurations { get; set; } = [];
}

/// <summary>Average time spent in a single pipeline stage.</summary>
public sealed class StageDuration
{
    public string Stage { get; set; } = string.Empty;
    public double AverageDays { get; set; }
}

/// <summary>A single point of the "hiring over time" series.</summary>
public sealed class TimePoint
{
    public string Period { get; set; } = string.Empty;
    public int Applications { get; set; }
    public int Hires { get; set; }
}

/// <summary>Actionable alert surfaced at the top of the dashboard.</summary>
public sealed class DashboardAlert
{
    /// <summary>One of: <c>info</c>, <c>success</c>, <c>warning</c>, <c>error</c>.</summary>
    public string Severity { get; set; } = "info";
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
