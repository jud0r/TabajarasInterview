using System.ComponentModel.DataAnnotations;

namespace TabajarasInterview.Web.DTOs;

/// <summary>
/// Request body for <c>POST /api/candidates/create</c> and
/// <c>PUT /api/candidates/update/{id}</c>. Mirrors the API's
/// <c>CreateCandidateRequest</c>/<c>UpdateCandidateRequest</c> (the <c>candidates</c> table).
/// </summary>
/// <remarks>
/// The update endpoint treats every field as optional and does not accept
/// <see cref="Email"/> — it is only sent on create.
/// </remarks>
public sealed class CandidateRequest
{
    [Required(ErrorMessage = "First name is required"),
     MinLength(3, ErrorMessage = "First name must be at least 3 characters")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required"),
     MinLength(3, ErrorMessage = "Last name must be at least 3 characters")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required"),
     EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }
}
