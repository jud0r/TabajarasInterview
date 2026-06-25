namespace TabajarasInterview.Web.DTOs;

/// <summary>
/// Response body for <c>POST /api/auth/login</c>.
/// </summary>
public sealed class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public UserResponse User { get; set; } = new();
}
