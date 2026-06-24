namespace TabajarasInterview.Web.DTOs;

/// <summary>
/// Response body for <c>POST /api/auth/refresh</c>.
/// </summary>
public sealed record RefreshResponse(
    string AccessToken,
    long ExpiresIn,
    string RefreshToken);
