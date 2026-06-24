using System.ComponentModel.DataAnnotations;

namespace TabajarasInterview.Web.DTOs;

/// <summary>
/// Request body for <c>POST /api/auth/login</c>.
/// </summary>
public sealed class LoginRequest
{
    [Required(ErrorMessage = "Email is required"),
     EmailAddress(ErrorMessage = "Invalid email format"),
     MaxLength(30, ErrorMessage = "Email cannot exceed 30 characters")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required"),
     MinLength(8, ErrorMessage = "Password must be at least 8 characters long"),
     MaxLength(30, ErrorMessage = "Password cannot exceed 30 characters")]
    public string Password { get; set; } = string.Empty;
}
