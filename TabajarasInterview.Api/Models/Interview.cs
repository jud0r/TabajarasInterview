namespace TabajarasInterview.Api.Models;

/// <summary>
/// Interview of a candidate application. Maps to the <c>interviews</c> table.
/// </summary>
public class Interview : SoftDeleteEntity
{
    /// <summary>Id of the <see cref="CandidateApplication"/>.</summary>
    public int CandidateApplicationId { get; set; }

    /// <summary>Id of the interviewer (<see cref="User"/>).</summary>
    public int InterviewerId { get; set; }

    /// <summary>Interview name.</summary>
    public required string Name { get; set; }

    /// <summary>Interview type (max 15 characters).</summary>
    public required string InterviewType { get; set; }

    /// <summary>Interview status (max 15 characters).</summary>
    public required string Status { get; set; }

    /// <summary>Score, <c>decimal(5,2)</c>.</summary>
    public decimal? Score { get; set; }

    /// <summary>Comments.</summary>
    public string? Comments { get; set; }

    /// <summary>Related application.</summary>
    public CandidateApplication? CandidateApplication { get; set; }

    /// <summary>Interviewer.</summary>
    public User? Interviewer { get; set; }

    /// <summary>Questions of this interview.</summary>
    public ICollection<InterviewQuestion> InterviewQuestions { get; set; } = [];

    /// <summary>Reviewers of this interview.</summary>
    public ICollection<InterviewReviewer> InterviewReviewers { get; set; } = [];
}
