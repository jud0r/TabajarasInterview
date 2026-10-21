using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace TabajarasInterview.Api.Auth;

/// <summary>
/// Port of Rust <c>AuthUser</c> extractor. Reproduces its exact status codes and plain-text messages:
/// 401 "Missing authorization header", 400 "Invalid authorization header",
/// 401 "Invalid Bearer token format", 401 "Invalid token", 401 "Invalid token type".
/// </summary>
public sealed class ApiBearerHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    JwtTokenService jwt)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Bearer";

    private const string FailureKey = "ApiBearer.Failure";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var values) || values.Count == 0)
        {
            return Fail(StatusCodes.Status401Unauthorized, "Missing authorization header");
        }

        var header = values[0]!;

        // HeaderValue::to_str only accepts visible ASCII (and tab).
        if (header.Any(c => !(c == '\t' || (c >= ' ' && c <= '~'))))
        {
            return Fail(StatusCodes.Status400BadRequest, "Invalid authorization header");
        }

        const string prefix = "Bearer ";
        if (!header.StartsWith(prefix, StringComparison.Ordinal))
        {
            return Fail(StatusCodes.Status401Unauthorized, "Invalid Bearer token format");
        }

        var claims = await jwt.DecodeTokenAsync(header[prefix.Length..]);
        if (claims is null)
        {
            return Fail(StatusCodes.Status401Unauthorized, "Invalid token");
        }

        if (claims.TokenType != TokenType.Access)
        {
            return Fail(StatusCodes.Status401Unauthorized, "Invalid token type");
        }

        var identity = new ClaimsIdentity(
            [
                new Claim("sub", claims.Sub),
                new Claim("id", claims.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new Claim("token_type", claims.TokenType.ToClaimValue()),
            ],
            SchemeName,
            nameType: "sub",
            roleType: ClaimTypes.Role);

        return AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        var (status, message) = Context.Items[FailureKey] is (int s, string m)
            ? (s, m)
            : (StatusCodes.Status401Unauthorized, "Missing authorization header");

        Response.StatusCode = status;
        Response.ContentType = "text/plain; charset=utf-8";
        await Response.WriteAsync(message);
    }

    private AuthenticateResult Fail(int status, string message)
    {
        Context.Items[FailureKey] = (status, message);
        return AuthenticateResult.Fail(message);
    }
}
