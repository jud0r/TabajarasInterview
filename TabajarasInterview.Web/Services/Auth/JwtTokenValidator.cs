using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace TabajarasInterview.Web.Services.Auth
{
    /// <summary>
    /// Validates the HS256 access token issued by the rust-api and produces a normalized
    /// <see cref="ClaimsPrincipal"/>. Shared by the server-side JwtBearer handler and the
    /// Blazor <see cref="CustomAuthenticationStateProviderService"/> so that the prerender
    /// (<c>HttpContext.User</c>) and interactive auth states are validated identically and
    /// stay in sync.
    /// </summary>
    public sealed class JwtTokenValidator
    {
        public const string AuthenticationType = "jwt";
        public const string AccessTokenType = "access";

        private const string SubClaim = "sub";
        private const string IdClaim = "id";
        private const string TokenTypeClaim = "token_type";

        private readonly TokenValidationParameters _parameters;

        public JwtTokenValidator(IConfiguration configuration)
        {
            var secret = configuration["Jwt:Secret"];
            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new InvalidOperationException(
                    "Missing 'Jwt:Secret'. Provide the same HS256 signing secret the rust-api uses " +
                    "via the AppHost 'jwt-secret' parameter, user-secrets, or appsettings.");
            }

            _parameters = new TokenValidationParameters
            {
                // The rust-api signs with HS256 using a symmetric secret and sets no
                // issuer/audience, so only the signature and lifetime are validated.
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                // Mirror the ~60s leeway the Rust jsonwebtoken validator allows.
                ClockSkew = TimeSpan.FromSeconds(60),
                NameClaimType = ClaimTypes.Name
            };
        }

        /// <summary>
        /// Validation parameters used by the JwtBearer handler. Exposed from one place so the
        /// handler and the auth-state provider validate tokens identically. Returns a clone so
        /// the handler can adjust its copy without mutating the shared instance.
        /// </summary>
        public TokenValidationParameters Parameters => _parameters.Clone();

        /// <summary>
        /// Validates a raw access token and returns a normalized principal, or <c>null</c> when
        /// the token is missing, malformed, expired, has an invalid signature, or is not an
        /// access token.
        /// </summary>
        public async Task<ClaimsPrincipal?> ValidateAsync(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            var handler = new JsonWebTokenHandler();
            var result = await handler.ValidateTokenAsync(token, _parameters);
            if (!result.IsValid || result.ClaimsIdentity is null)
                return null;

            return BuildPrincipal(result.ClaimsIdentity);
        }

        /// <summary>
        /// Normalizes the raw token claims (<c>sub</c>, <c>id</c>, <c>token_type</c>) into a
        /// <see cref="ClaimsPrincipal"/> using standard claim types, and enforces that the token
        /// is an access token (mirroring the rust-api extractor). Returns <c>null</c> for refresh
        /// or otherwise non-access tokens.
        /// </summary>
        public ClaimsPrincipal? BuildPrincipal(ClaimsIdentity source)
        {
            var tokenType = source.FindFirst(TokenTypeClaim)?.Value;
            if (!string.Equals(tokenType, AccessTokenType, StringComparison.OrdinalIgnoreCase))
                return null;

            var email = source.FindFirst(SubClaim)?.Value;
            var id = source.FindFirst(IdClaim)?.Value;

            var identity = new ClaimsIdentity(AuthenticationType, ClaimTypes.Name, ClaimTypes.Role);

            if (!string.IsNullOrEmpty(id))
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, id));

            if (!string.IsNullOrEmpty(email))
            {
                identity.AddClaim(new Claim(ClaimTypes.Email, email));
                identity.AddClaim(new Claim(ClaimTypes.Name, email));
            }

            identity.AddClaim(new Claim(TokenTypeClaim, AccessTokenType));

            return new ClaimsPrincipal(identity);
        }
    }
}
