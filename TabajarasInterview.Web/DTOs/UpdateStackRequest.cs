using System.ComponentModel.DataAnnotations;

namespace TabajarasInterview.Web.DTOs;

/// <summary>
/// Request body for <c>PUT /api/stacks/update/{id}</c>. Mirrors the API's
/// <c>UpdateStackRequest</c> (the <c>stacks</c> table).
/// </summary>
/// <remarks>
/// The API treats every field as optional, but the UI always requires a
/// non-empty <see cref="Name"/> so users can't blank out a stack's name.
/// </remarks>
public sealed class UpdateStackRequest
{
    [Required(ErrorMessage = "Name is required"),
     StringLength(100, ErrorMessage = "Name must be at most 100 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description must be at most 500 characters")]
    public string? Description { get; set; }
}
