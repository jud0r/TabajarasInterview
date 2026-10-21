using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace TabajarasInterview.Api.Auth;

/// <summary>Port of Rust <c>auth/jwt.rs</c>: HS256 tokens with claims sub, exp, id, token_type.</summary>
public sealed class JwtTokenService
{
    private static readonly JsonWebTokenHandler Handler = new();

    private readonly JwtOptions _options;
    private readonly SymmetricSecurityKey _key;
    private readonly TimeProvider _time;

    public JwtTokenService(IOptions<JwtOptions> options, TimeProvider time)
    {
        _options = options.Value;
        _time = time;
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
    }

    public GeneratedToken GenerateToken(string email, int id) =>
        Generate(email, id, TokenType.Access, _options.ExpirationInMinutes * 60);

    public GeneratedToken GenerateRefreshToken(string email, int id) =>
        Generate(email, id, TokenType.Refresh, _options.RefreshTokenDays * 24 * 60 * 60);

    private GeneratedToken Generate(string email, int id, TokenType tokenType, int ttlSeconds)
    {
        var exp = _time.GetUtcNow().ToUnixTimeSeconds() + ttlSeconds;

        var descriptor = new SecurityTokenDescriptor
        {
            Claims = new Dictionary<string, object>
            {
                ["sub"] = email,
                ["exp"] = exp,
                ["id"] = id,
                ["token_type"] = tokenType.ToClaimValue(),
            },
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256),
        };

        return new GeneratedToken(Handler.CreateToken(descriptor), ttlSeconds);
    }

    /// <summary>Returns null for any invalid token (bad signature, expired, missing/invalid claims).</summary>
    public async Task<JwtClaims?> DecodeTokenAsync(string token)
    {
        // Rust Validation::default(): HS256, exp required and checked with 60s leeway, no iss/aud checks.
        var parameters = new TokenValidationParameters
        {
            IssuerSigningKey = _key,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromSeconds(60),
        };

        var result = await Handler.ValidateTokenAsync(token, parameters);
        if (!result.IsValid || result.SecurityToken is not JsonWebToken jwt)
        {
            return null;
        }

        if (!jwt.TryGetPayloadValue<string>("sub", out var sub) || sub is null)
        {
            return null;
        }

        if (!jwt.TryGetPayloadValue<int>("id", out var id))
        {
            return null;
        }

        var tokenType = TokenType.Access;
        if (jwt.TryGetPayloadValue<string>("token_type", out var rawType))
        {
            if (!TokenTypeExtensions.TryParseClaimValue(rawType, out tokenType))
            {
                return null;
            }
        }

        return new JwtClaims(sub, id, tokenType);
    }

    /// <summary>Lowercase hex SHA-256, used to store/look up refresh tokens.</summary>
    public static string HashToken(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
