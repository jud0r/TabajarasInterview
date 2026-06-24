namespace TabajarasInterview.Web.DTOs;

/// <summary>
/// Request body for <c>POST /api/auth/login</c>.
/// </summary>
public sealed record LoginRequest(string Email, string Password);
