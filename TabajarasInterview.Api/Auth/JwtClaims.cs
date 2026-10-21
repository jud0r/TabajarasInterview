namespace TabajarasInterview.Api.Auth;

/// <summary>Rust <c>TokenType</c>; serialized as snake_case ("access" / "refresh").</summary>
public enum TokenType
{
    Access,
    Refresh,
}

public static class TokenTypeExtensions
{
    public static string ToClaimValue(this TokenType type) => type switch
    {
        TokenType.Access => "access",
        TokenType.Refresh => "refresh",
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    public static bool TryParseClaimValue(string? value, out TokenType type)
    {
        switch (value)
        {
            case "access": type = TokenType.Access; return true;
            case "refresh": type = TokenType.Refresh; return true;
            default: type = default; return false;
        }
    }
}

/// <summary>Rust <c>Claims</c> (sub = email, id = user id).</summary>
public sealed record JwtClaims(string Sub, int Id, TokenType TokenType);

/// <summary>A generated token and its lifetime in seconds (Rust returns <c>(String, i32)</c>).</summary>
public sealed record GeneratedToken(string Token, int ExpiresInSeconds);
