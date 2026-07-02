namespace TabajarasInterview.Web.DTOs.Candidate;

/// <summary>
/// A candidate's application to a single position, joined with its position,
/// tech stacks and interviews. This is the shape returned by
/// <c>GET /api/candidates/{id}/positions</c> (which internally calls the
/// <c>get_candidate_positions(candidate_id)</c> data source). Property names map
/// to the API's snake_case JSON through
/// <see cref="Services.Api.ApiResponseParserService"/>.
/// </summary>
/// <remarks>
/// Every optional field is nullable so the UI degrades gracefully when the API
/// omits data (e.g. an application still in progress has no <see cref="FinishedAt"/>
/// or <see cref="FinalScore"/>).
/// </remarks>
public sealed class CandidateApplicationResponse
{
    /// <summary>Maps to <c>candidate_applications.id</c>.</summary>
    public int Id { get; set; }

    public int CandidateId { get; set; }

    public int PositionId { get; set; }

    /// <summary>Position title (from the joined <c>positions</c> row).</summary>
    public string PositionTitle { get; set; } = string.Empty;

    /// <summary>Position lifecycle status (Draft, Open, OnHold, Closed, Cancelled...).</summary>
    public string? PositionStatus { get; set; }

    /// <summary>Application status (Applied, Screening, Interviewing, OfferExtended, Hired, Rejected, Withdrawn...).</summary>
    public string? Status { get; set; }

    /// <summary>Maps to <c>candidate_applications.started_at</c>.</summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>Maps to <c>candidate_applications.finished_at</c> (null while in progress).</summary>
    public DateTime? FinishedAt { get; set; }

    /// <summary>Maps to <c>candidate_applications.final_score</c>.</summary>
    public double? FinalScore { get; set; }

    /// <summary>Maps to <c>candidate_applications.final_comments</c>.</summary>
    public string? FinalComments { get; set; }

    /// <summary>Tech stacks linked to the position (via <c>position_stacks</c>).</summary>
    public List<StackRef> Stacks { get; set; } = [];

    /// <summary>Interviews belonging to this application.</summary>
    public List<InterviewResponse> Interviews { get; set; } = [];

    /// <summary>Number of interviews attached to the application.</summary>
    public int InterviewCount => Interviews.Count;

    /// <summary>
    /// Elapsed hiring time between <see cref="StartedAt"/> and
    /// <see cref="FinishedAt"/>. Null when either endpoint is missing.
    /// </summary>
    public TimeSpan? HiringDuration =>
        StartedAt is { } start && FinishedAt is { } finish && finish >= start
            ? finish - start
            : null;

    /// <summary>True for terminal application statuses (no further action expected).</summary>
    public bool IsClosed => FinishedAt is not null
        || CandidateStatusKind.IsTerminalApplicationStatus(Status);

    /// <summary>
    /// Best-effort "current stage" label: the application status, falling back to
    /// the most recent interview type when the status is unknown/empty.
    /// </summary>
    public string CurrentStage =>
        !string.IsNullOrWhiteSpace(Status)
            ? Status!
            : Interviews.Count > 0
                ? (Interviews[^1].Type ?? "In progress")
                : "Applied";
}

/// <summary>
/// A single interview (maps to the <c>interviews</c> table) with its reviewers
/// and asked questions. Belongs to one <see cref="CandidateApplicationResponse"/>.
/// </summary>
public sealed class InterviewResponse
{
    public int Id { get; set; }

    public int ApplicationId { get; set; }

    /// <summary>Display name of the interview (e.g. "Technical round 1").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Interview type (Screening, Technical, Behavioral, Final...).</summary>
    public string? Type { get; set; }

    /// <summary>Interview status (Scheduled, InProgress, Completed, Cancelled, NoShow, Rescheduled...).</summary>
    public string? Status { get; set; }

    /// <summary>Maps to <c>interviews.interviewer_id</c> (a <c>users</c> row).</summary>
    public int? InterviewerId { get; set; }

    /// <summary>Interviewer display name (from the joined <c>users</c> row).</summary>
    public string? InterviewerName { get; set; }

    /// <summary>Maps to <c>interviews.score</c>.</summary>
    public double? Score { get; set; }

    /// <summary>Maps to <c>interviews.comments</c>.</summary>
    public string? Comments { get; set; }

    /// <summary>Maps to <c>interviews.created_at</c>.</summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>Maps to <c>interviews.updated_at</c>.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Reviewer feedback (maps to <c>interview_reviewers</c>).</summary>
    public List<InterviewReviewerResponse> Reviewers { get; set; } = [];

    /// <summary>Questions asked during the interview (maps to <c>interview_questions</c>).</summary>
    public List<InterviewQuestionResponse> Questions { get; set; } = [];

    /// <summary>True when the interview reached a scored/finished state.</summary>
    public bool IsCompleted => CandidateStatusKind.IsCompletedInterviewStatus(Status);

    /// <summary>True while the interview is still expected to happen.</summary>
    public bool IsPending => CandidateStatusKind.IsPendingInterviewStatus(Status);
}

/// <summary>
/// Reviewer feedback for an interview (maps to <c>interview_reviewers</c> joined
/// with the reviewer's <c>users</c> row).
/// </summary>
public sealed class InterviewReviewerResponse
{
    public int ReviewerId { get; set; }

    /// <summary>Reviewer display name.</summary>
    public string ReviewerName { get; set; } = string.Empty;

    public double? Score { get; set; }

    public string? Comments { get; set; }

    /// <summary>When the review was submitted.</summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>True when no score/comment was provided yet.</summary>
    public bool IsMissing => Score is null && string.IsNullOrWhiteSpace(Comments);
}

/// <summary>
/// A question asked during an interview (maps to <c>interview_questions</c>
/// joined with <c>questions</c> and, optionally, its <c>stacks</c> row).
/// </summary>
public sealed class InterviewQuestionResponse
{
    public int QuestionId { get; set; }

    /// <summary>Question text (from the joined <c>questions</c> row).</summary>
    public string Question { get; set; } = string.Empty;

    /// <summary>Presentation order within the interview.</summary>
    public int Order { get; set; }

    /// <summary>Score awarded for the answer.</summary>
    public double? Score { get; set; }

    /// <summary>Related tech stack name (optional).</summary>
    public string? Stack { get; set; }

    /// <summary>Acceptable/expected answer, when available.</summary>
    public string? AcceptableAnswer { get; set; }
}

/// <summary>Lightweight reference to a tech stack (maps to <c>stacks</c>).</summary>
public sealed class StackRef
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// A single event on the candidate's recruitment timeline. Returned by
/// <c>GET /api/candidates/{id}/timeline</c>. When the endpoint is unavailable the
/// timeline is synthesized client-side from the applications payload.
/// </summary>
public sealed class TimelineEvent
{
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Machine-friendly event type: <c>candidate_created</c>, <c>application_started</c>,
    /// <c>interview_created</c>, <c>interview_completed</c>, <c>application_finished</c>,
    /// <c>final_decision</c>.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Related position title, when the event belongs to an application.</summary>
    public string? Position { get; set; }

    /// <summary>Related interview name, when the event belongs to an interview.</summary>
    public string? Interview { get; set; }

    /// <summary>Optional status shown as a badge on the timeline item.</summary>
    public string? Status { get; set; }
}

/// <summary>
/// KPI figures for a single candidate, computed client-side from the applications
/// payload so no extra API round-trips are required.
/// </summary>
public sealed class CandidateProfileStats
{
    public int TotalApplications { get; set; }
    public int ActiveApplications { get; set; }
    public int CompletedApplications { get; set; }
    public int RejectedApplications { get; set; }
    public double? AverageFinalScore { get; set; }
    public double? AverageInterviewScore { get; set; }
    public int InterviewsCompleted { get; set; }
    public int PendingInterviews { get; set; }

    /// <summary>Builds the aggregate from the candidate's applications.</summary>
    public static CandidateProfileStats From(IReadOnlyCollection<CandidateApplicationResponse> applications)
    {
        var interviews = applications.SelectMany(a => a.Interviews).ToList();

        var finalScores = applications
            .Where(a => a.FinalScore is not null)
            .Select(a => a.FinalScore!.Value)
            .ToList();

        var interviewScores = interviews
            .Where(i => i.Score is not null)
            .Select(i => i.Score!.Value)
            .ToList();

        return new CandidateProfileStats
        {
            TotalApplications = applications.Count,
            ActiveApplications = applications.Count(a => !a.IsClosed),
            CompletedApplications = applications.Count(a => a.IsClosed
                && !CandidateStatusKind.IsRejectedApplicationStatus(a.Status)),
            RejectedApplications = applications.Count(a =>
                CandidateStatusKind.IsRejectedApplicationStatus(a.Status)),
            AverageFinalScore = finalScores.Count > 0 ? finalScores.Average() : null,
            AverageInterviewScore = interviewScores.Count > 0 ? interviewScores.Average() : null,
            InterviewsCompleted = interviews.Count(i => i.IsCompleted),
            PendingInterviews = interviews.Count(i => i.IsPending),
        };
    }
}

/// <summary>
/// Central, defensive status classification shared by the DTOs and UI. Every
/// comparison normalizes the raw value (case/space/underscore/hyphen insensitive)
/// so unknown or slightly-different status strings are handled safely.
/// </summary>
public static class CandidateStatusKind
{
    /// <summary>Normalizes a status for comparison: lowercase, no separators.</summary>
    public static string Normalize(string? status) =>
        new string((status ?? string.Empty)
            .Where(char.IsLetterOrDigit)
            .ToArray())
            .ToLowerInvariant();

    public static bool IsTerminalApplicationStatus(string? status) =>
        Normalize(status) is "hired" or "rejected" or "withdrawn" or "declined" or "closed";

    public static bool IsRejectedApplicationStatus(string? status) =>
        Normalize(status) is "rejected" or "declined" or "withdrawn";

    public static bool IsCompletedInterviewStatus(string? status) =>
        Normalize(status) is "completed" or "done" or "passed" or "reviewed";

    public static bool IsPendingInterviewStatus(string? status) =>
        Normalize(status) is "scheduled" or "inprogress" or "rescheduled" or "pending";
}
