namespace TabajarasInterview.Web.DTOs;

/// <summary>
/// Question returned by the API (mirrors the API's <c>QuestionResponse</c>
/// and the <c>questions</c> table). Property names map to the API's camelCase
/// JSON through <see cref="Services.Api.ApiResponseParserService"/>.
/// </summary>
public sealed class QuestionResponse
{
    public int Id { get; set; }

    /// <summary>Id of the single technology stack this question belongs to. Maps to <c>stackId</c>.</summary>
    public int StackId { get; set; }

    /// <summary>The interview question text.</summary>
    public string Question { get; set; } = string.Empty;

    /// <summary>Maps to <c>acceptableAnswer</c>.</summary>
    public string? AcceptableAnswer { get; set; }

    /// <summary>Difficulty level (Junior, Mid or Senior).</summary>
    public string? Level { get; set; }

    /// <summary>Maps to <c>createdAt</c>.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Maps to <c>updatedAt</c> (null until the question is edited).</summary>
    public DateTime? UpdatedAt { get; set; }
}
