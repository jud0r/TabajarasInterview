using Microsoft.EntityFrameworkCore;
using TabajarasInterview.Api.Auth;
using TabajarasInterview.Api.Common;
using TabajarasInterview.Api.Data;
using TabajarasInterview.Api.Dtos;
using TabajarasInterview.Api.Models;

namespace TabajarasInterview.Api.Endpoints;

/// <summary>Port of Rust <c>handlers/auth.rs</c>, mounted at <c>/api/auth</c>.</summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("auth")
            .AddEndpointFilter<ApiErrorFilter>();

        group.MapPost("/login", Login)
            .Produces<LoginResponse>()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/refresh", Refresh)
            .Produces<RefreshResponse>()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/logout", Logout)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<IResult> Login(
        LoginRequest payload,
        AppDbContext db,
        JwtTokenService jwt,
        TimeProvider time,
        CancellationToken ct)
    {
        // Soft-deleted users are excluded by the global query filter (Rust: DeletedAt IS NULL).
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == payload.Email, ct)
            ?? throw new ApiException(StatusCodes.Status401Unauthorized, "Invalid credentials");

        bool passwordOk;
        try
        {
            passwordOk = BCrypt.Net.BCrypt.Verify(payload.Password, user.PasswordHash);
        }
        catch (Exception e) when (e is ArgumentException or FormatException or BCrypt.Net.SaltParseException)
        {
            throw new ApiException(StatusCodes.Status500InternalServerError, "Invalid credentials");
        }

        if (!passwordOk)
        {
            throw new ApiException(StatusCodes.Status401Unauthorized, "Invalid credentials");
        }

        var accessToken = jwt.GenerateToken(user.Email, user.Id);
        var refreshToken = jwt.GenerateRefreshToken(user.Email, user.Id);

        await StoreRefreshTokenAsync(db, time, user.Id, refreshToken.Token, refreshToken.ExpiresInSeconds, ct);

        return Results.Ok(new LoginResponse(
            accessToken.Token,
            accessToken.ExpiresInSeconds,
            refreshToken.Token,
            UserResponse.From(user)));
    }

    private static async Task<IResult> Refresh(
        RefreshRequest payload,
        AppDbContext db,
        JwtTokenService jwt,
        TimeProvider time,
        CancellationToken ct)
    {
        var claims = await jwt.DecodeTokenAsync(payload.RefreshToken)
            ?? throw new ApiException(StatusCodes.Status401Unauthorized, "Invalid refresh token");

        if (claims.TokenType != TokenType.Refresh)
        {
            throw new ApiException(StatusCodes.Status401Unauthorized, "Invalid token type");
        }

        var tokenHash = JwtTokenService.HashToken(payload.RefreshToken);

        var stored = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.RevokedAt == null, ct)
            ?? throw new ApiException(StatusCodes.Status401Unauthorized, "Invalid refresh token");

        var now = time.GetUtcNow().UtcDateTime;

        if (stored.ExpiresAt < now)
        {
            throw new ApiException(StatusCodes.Status401Unauthorized, "Refresh token expired");
        }

        // Rotate: revoke the used refresh token before issuing a new one.
        stored.RevokedAt = now;
        await db.SaveChangesAsync(ct);

        var accessToken = jwt.GenerateToken(claims.Sub, claims.Id);
        var refreshToken = jwt.GenerateRefreshToken(claims.Sub, claims.Id);

        await StoreRefreshTokenAsync(db, time, claims.Id, refreshToken.Token, refreshToken.ExpiresInSeconds, ct);

        return Results.Ok(new RefreshResponse(accessToken.Token, accessToken.ExpiresInSeconds, refreshToken.Token));
    }

    private static async Task<IResult> Logout(
        LogoutRequest payload,
        AppDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        var tokenHash = JwtTokenService.HashToken(payload.RefreshToken);

        var stored = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.RevokedAt == null, ct)
            ?? throw new ApiException(StatusCodes.Status401Unauthorized, "Invalid refresh token");

        stored.RevokedAt = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }

    /// <summary>Persists the SHA-256 hash of a refresh token so it can be validated/revoked later.</summary>
    private static async Task StoreRefreshTokenAsync(
        AppDbContext db,
        TimeProvider time,
        int userId,
        string token,
        int expiresInSeconds,
        CancellationToken ct)
    {
        var now = time.GetUtcNow().UtcDateTime;

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            TokenHash = JwtTokenService.HashToken(token),
            ExpiresAt = now.AddSeconds(expiresInSeconds),
            CreatedAt = now,
        });

        await db.SaveChangesAsync(ct);
    }
}
