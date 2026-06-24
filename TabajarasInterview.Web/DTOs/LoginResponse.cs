namespace TabajarasInterview.Web.DTOs;

/// <summary>
/// Response body for <c>POST /api/auth/login</c>.
/// </summary>
public sealed record LoginResponse(
    string AccessToken,
    long ExpiresIn,
    string RefreshToken,
    UserResponse User);
