namespace TabajarasInterview.Api.Models;

/// <summary>
/// Interview question. Maps to the <c>questions</c> table.
/// </summary>
public class Question : SoftDeleteEntity
{
    /// <summary>Id of the owning <see cref="Stack"/>.</summary>
    public int StackId { get; set; }

    /// <summary>Question text.</summary>
    public required string Text { get; set; }

    /// <summary>Optional acceptable answer.</summary>
    public string? AcceptableAnswer { get; set; }

    /// <summary>Optional level: Junior, Mid or Senior (max 10 characters).</summary>
    public string? Level { get; set; }

    /// <summary>Owning stack.</summary>
    public Stack? Stack { get; set; }

    /// <summary>Interview usages of this question.</summary>
    public ICollection<InterviewQuestion> InterviewQuestions { get; set; } = [];
}
