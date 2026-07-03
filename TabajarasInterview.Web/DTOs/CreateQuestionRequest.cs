using System.ComponentModel.DataAnnotations;

namespace TabajarasInterview.Web.DTOs;

/// <summary>
/// Request body for <c>POST /api/questions/create</c>. Mirrors the API's
/// <c>CreateQuestionRequest</c> (the <c>questions</c> table).
/// </summary>
/// <remarks>
/// The rust-api only requires <see cref="StackId"/> and <see cref="Question"/>,
/// but the Blazor form always requires an acceptable answer and a level so
/// interviewers can't create incomplete questions.
/// </remarks>
public sealed class CreateQuestionRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Stack is required")]
    public int StackId { get; set; }

    [Required(ErrorMessage = "Question is required"),
     StringLength(1000, ErrorMessage = "Question must be at most 1000 characters")]
    public string Question { get; set; } = string.Empty;

    [Required(ErrorMessage = "Acceptable answer is required"),
     StringLength(2000, ErrorMessage = "Acceptable answer must be at most 2000 characters")]
    public string? AcceptableAnswer { get; set; }

    [Required(ErrorMessage = "Level is required")]
    public string? Level { get; set; }
}
