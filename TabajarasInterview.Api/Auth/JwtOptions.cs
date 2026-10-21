namespace TabajarasInterview.Api.Auth;

/// <summary>
/// JWT settings. Replaces the Rust env vars SECRET, TOKEN_EXPIRATION_IN_MINUTES (default 60)
/// and REFRESH_TOKEN_EXPIRATION_IN_DAYS (default 30).
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;

    public int ExpirationInMinutes { get; set; } = 60;

    public int RefreshTokenDays { get; set; } = 30;
}
