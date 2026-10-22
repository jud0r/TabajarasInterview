namespace TabajarasInterview.Api.Models;

/// <summary>
/// Reviewer assigned to an interview. Maps to the <c>interview_reviewers</c> table.
/// </summary>
public class InterviewReviewer : SoftDeleteEntity
{
    /// <summary>Id of the <see cref="Interview"/>.</summary>
    public int InterviewId { get; set; }

    /// <summary>Id of the reviewer (<see cref="User"/>).</summary>
    public int ReviewerId { get; set; }

    /// <summary>Review score, <c>decimal(5,2)</c>.</summary>
    public decimal? ReviewScore { get; set; }

    /// <summary>Review comments.</summary>
    public string? ReviewComments { get; set; }

    /// <summary>Reviewed interview.</summary>
    public Interview? Interview { get; set; }

    /// <summary>Reviewer user.</summary>
    public User? Reviewer { get; set; }
}
