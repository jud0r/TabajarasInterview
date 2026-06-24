namespace TabajarasInterview.Web.DTOs;

/// <summary>
/// Response body for <c>POST /api/auth/refresh</c>.
/// </summary>
public sealed class RefreshResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public long ExpiresIn { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
}
