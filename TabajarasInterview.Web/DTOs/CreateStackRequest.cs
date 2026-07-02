using System.ComponentModel.DataAnnotations;

namespace TabajarasInterview.Web.DTOs;

/// <summary>
/// Request body for <c>POST /api/stacks/create</c>. Mirrors the API's
/// <c>CreateStackRequest</c> (the <c>stacks</c> table).
/// </summary>
public sealed class CreateStackRequest
{
    [Required(ErrorMessage = "Name is required"),
     StringLength(100, ErrorMessage = "Name must be at most 100 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description must be at most 500 characters")]
    public string? Description { get; set; }
}
