using System.ComponentModel.DataAnnotations;

namespace TabajarasInterview.Web.DTOs;

/// <summary>
/// Request body for <c>PUT /api/questions/update/{id}</c>. Mirrors the API's
/// <c>UpdateQuestionRequest</c> (the <c>questions</c> table).
/// </summary>
/// <remarks>
/// The rust-api treats every field as optional, but the UI always submits a
/// complete form, so every field is required here (same approach as
/// <see cref="UpdateStackRequest"/>).
/// </remarks>
public sealed class UpdateQuestionRequest
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
