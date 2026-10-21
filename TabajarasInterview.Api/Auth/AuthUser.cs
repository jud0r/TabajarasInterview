using System.Security.Claims;

namespace TabajarasInterview.Api.Auth;

/// <summary>Port of Rust <c>AuthUser</c>; the authenticated access-token claims.</summary>
public sealed record AuthUser(string Email, int Id)
{
    public static AuthUser From(ClaimsPrincipal principal) =>
        new(
            principal.FindFirstValue("sub") ?? string.Empty,
            int.Parse(principal.FindFirstValue("id")!, System.Globalization.CultureInfo.InvariantCulture));
}

public static class AuthUserExtensions
{
    /// <summary>Gets the authenticated user. Only valid on endpoints that call <c>RequireAuthorization()</c>.</summary>
    public static AuthUser GetAuthUser(this ClaimsPrincipal principal) => AuthUser.From(principal);
}
