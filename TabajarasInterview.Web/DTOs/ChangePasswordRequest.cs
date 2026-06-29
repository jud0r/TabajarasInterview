using System.ComponentModel.DataAnnotations;

namespace TabajarasInterview.Web.DTOs;

/// <summary>
/// Request body for <c>PUT /api/users/password</c>. Mirrors the API's ChangePasswordRequest.
/// </summary>
public sealed class ChangePasswordRequest
{
    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required"),
     MinLength(8, ErrorMessage = "New password must be at least 8 characters long"),
     MaxLength(100, ErrorMessage = "New password cannot exceed 100 characters")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your new password"),
     Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
