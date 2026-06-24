namespace TabajarasInterview.Web.DTOs;

/// <summary>
/// Request body for <c>POST /api/auth/refresh</c>.
/// </summary>
public sealed record RefreshRequest(string RefreshToken);
