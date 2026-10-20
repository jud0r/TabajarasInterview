namespace TabajarasInterview.Api.Models;

/// <summary>
/// Question asked in an interview. Maps to the <c>interview_questions</c> table.
/// </summary>
public class InterviewQuestion : SoftDeleteEntity
{
    /// <summary>Id of the <see cref="Interview"/>.</summary>
    public int InterviewId { get; set; }

    /// <summary>Id of the <see cref="Question"/>.</summary>
    public int QuestionId { get; set; }

    /// <summary>Order of the question within the interview (unique per interview).</summary>
    public int QuestionOrder { get; set; }

    /// <summary>Score, <c>decimal(5,2)</c>.</summary>
    public decimal? Score { get; set; }

    /// <summary>Owning interview.</summary>
    public Interview? Interview { get; set; }

    /// <summary>Asked question.</summary>
    public Question? Question { get; set; }
}
