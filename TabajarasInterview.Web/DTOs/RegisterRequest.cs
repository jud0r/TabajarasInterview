using System.ComponentModel.DataAnnotations;

namespace TabajarasInterview.Web.DTOs;

/// <summary>
/// Request body for <c>POST /api/users/create</c>. Mirrors the API's CreateUserRequest.
/// </summary>
public sealed class RegisterRequest
{
    [Required(ErrorMessage = "First name is required"),
     MinLength(3, ErrorMessage = "First name must be at least 3 characters"),
     MaxLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required"),
     MinLength(3, ErrorMessage = "Last name must be at least 3 characters"),
     MaxLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required"),
     EmailAddress(ErrorMessage = "Invalid email format"),
     MaxLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required"),
     MinLength(8, ErrorMessage = "Password must be at least 8 characters long"),
     MaxLength(100, ErrorMessage = "Password cannot exceed 100 characters")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password"),
     Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
