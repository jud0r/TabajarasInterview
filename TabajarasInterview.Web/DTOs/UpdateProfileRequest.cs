using System.ComponentModel.DataAnnotations;

namespace TabajarasInterview.Web.DTOs;

/// <summary>
/// Request body for <c>PUT /api/users/update</c>. Mirrors the API's UpdateUserRequest.
/// </summary>
public sealed class UpdateProfileRequest
{
    [Required(ErrorMessage = "First name is required"),
     MinLength(3, ErrorMessage = "First name must be at least 3 characters"),
     MaxLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required"),
     MinLength(3, ErrorMessage = "Last name must be at least 3 characters"),
     MaxLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
    public string LastName { get; set; } = string.Empty;
}
