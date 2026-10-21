using Microsoft.EntityFrameworkCore;
using TabajarasInterview.Api.Auth;
using TabajarasInterview.Api.Common;
using TabajarasInterview.Api.Data;
using TabajarasInterview.Api.Dtos;
using TabajarasInterview.Api.Models;

namespace TabajarasInterview.Api.Endpoints;

/// <summary>Port of Rust <c>handlers/users.rs</c>, mounted at <c>/api/users</c>.</summary>
public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("users")
            .AddEndpointFilter<ApiErrorFilter>();

        group.MapGet("/get_all", GetUsers)
            .RequireAuthorization()
            .Produces<List<UserResponse>>();

        group.MapPost("/create", CreateUser)
            .Produces<UserResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapGet("/get", GetUser)
            .RequireAuthorization()
            .Produces<UserResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/update", UpdateUser)
            .RequireAuthorization()
            .Produces<UserResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/password", ChangePassword)
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/delete", DeleteUser)
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetUsers(AppDbContext db, CancellationToken ct)
    {
        var users = await db.Users.AsNoTracking().ToListAsync(ct);
        return Results.Ok(users.Select(UserResponse.From));
    }

    private static async Task<IResult> CreateUser(CreateUserRequest payload, AppDbContext db, TimeProvider time, CancellationToken ct)
    {
        new RequestValidator()
            .MinLength("first_name", payload.FirstName, 3, "first_name must be at least 3 characters")
            .MinLength("last_name", payload.LastName, 3, "last_name must be at least 3 characters")
            .Email("email", payload.Email, "email must be a valid email address")
            .MinLength("password", payload.Password, 8, "password must be at least 8 characters")
            .ThrowIfInvalid();

        string passwordHash;
        try
        {
            passwordHash = BCrypt.Net.BCrypt.HashPassword(payload.Password, 12);
        }
        catch (ArgumentException)
        {
            throw new ApiException(StatusCodes.Status500InternalServerError, "Failed to hash password");
        }

        var user = new User
        {
            FirstName = payload.FirstName,
            LastName = payload.LastName,
            Email = payload.Email,
            PasswordHash = passwordHash,
            CreatedAt = time.GetUtcNow().UtcDateTime,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return Results.Json(UserResponse.From(user), statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> GetUser(HttpContext http, AppDbContext db, CancellationToken ct)
    {
        var id = http.User.GetAuthUser().Id;

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "User not found");

        return Results.Ok(UserResponse.From(user));
    }

    private static async Task<IResult> UpdateUser(
        UpdateUserRequest payload,
        HttpContext http,
        AppDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        new RequestValidator()
            .MinLength("first_name", payload.FirstName, 3, "first_name must be at least 3 characters")
            .MinLength("last_name", payload.LastName, 3, "last_name must be at least 3 characters")
            .ThrowIfInvalid();

        var id = http.User.GetAuthUser().Id;

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "User not found");

        if (payload.FirstName is not null)
        {
            user.FirstName = payload.FirstName;
        }

        if (payload.LastName is not null)
        {
            user.LastName = payload.LastName;
        }

        user.UpdatedAt = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);

        return Results.Ok(UserResponse.From(user));
    }

    private static async Task<IResult> ChangePassword(
        ChangePasswordRequest payload,
        HttpContext http,
        AppDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        new RequestValidator()
            .MinLength("current_password", payload.CurrentPassword, 8, "current_password must be at least 8 characters")
            .MinLength("new_password", payload.NewPassword, 8, "new_password must be at least 8 characters")
            .ThrowIfInvalid();

        var id = http.User.GetAuthUser().Id;

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "User not found");

        bool passwordOk;
        try
        {
            passwordOk = BCrypt.Net.BCrypt.Verify(payload.CurrentPassword, user.PasswordHash);
        }
        catch (Exception e) when (e is ArgumentException or FormatException or BCrypt.Net.SaltParseException)
        {
            throw new ApiException(StatusCodes.Status500InternalServerError, "Failed to verify password");
        }

        if (!passwordOk)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Current password is incorrect");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(payload.NewPassword, 12);
        user.UpdatedAt = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }

    private static async Task<IResult> DeleteUser(HttpContext http, AppDbContext db, TimeProvider time, CancellationToken ct)
    {
        var id = http.User.GetAuthUser().Id;

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "User not found");

        user.DeletedAt = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }
}
