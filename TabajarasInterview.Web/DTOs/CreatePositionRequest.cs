using System.ComponentModel.DataAnnotations;
using TabajarasInterview.Web.Components.Shared;

namespace TabajarasInterview.Web.DTOs;

/// <summary>
/// Request body for <c>POST /api/positions/create</c>. Mirrors the API's
/// <c>CreatePositionRequest</c> (the <c>positions</c> table).
/// </summary>
/// <remarks>
/// The rust-api requires <see cref="Title"/> and <see cref="Status"/>, but the
/// Blazor form always requires a description too so a position can't be
/// created without context for candidates/interviewers.
/// </remarks>
public sealed class CreatePositionRequest
{
    [Required(ErrorMessage = "Title is required"),
     StringLength(200, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 200 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required"),
     StringLength(2000, ErrorMessage = "Description must be at most 2000 characters")]
    public string? Description { get; set; }

    /// <summary>Wire-format status value (snake_case, e.g. <c>draft</c>, <c>open</c>). See <see cref="PositionStatus"/>.</summary>
    [Required(ErrorMessage = "Status is required")]
    public string Status { get; set; } = PositionStatus.Draft;

    /// <summary>Wire-format work model (e.g. <c>remote</c>). Maps to <c>work_model</c>. See <see cref="Components.Shared.WorkModel"/>.</summary>
    [Required(ErrorMessage = "Work model is required")]
    public string WorkModel { get; set; } = Components.Shared.WorkModel.Remote;
}
