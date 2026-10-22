using Microsoft.EntityFrameworkCore;
using TabajarasInterview.Api.Common;
using TabajarasInterview.Api.Data;
using TabajarasInterview.Api.Dtos;
using TabajarasInterview.Api.Models;

namespace TabajarasInterview.Api.Endpoints;

/// <summary>
/// Port of Rust <c>handlers/candidates.rs</c>, mounted at <c>/api/candidates</c>.
/// <c>GET /get/{id}/positions</c> is pending until <c>positions.rs</c> (PositionResponse) is migrated.
/// </summary>
public static class CandidateEndpoints
{
    public static IEndpointRouteBuilder MapCandidateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/candidates")
            .WithTags("candidates")
            .RequireAuthorization()
            .AddEndpointFilter<ApiErrorFilter>();

        group.MapGet("/getAll", GetCandidates)
            .Produces<List<CandidateResponse>>();

        group.MapPost("/create", CreateCandidate)
            .Produces<CandidateResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapGet("/get/{id:int}", GetCandidate)
            .Produces<CandidateResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/update/{id:int}", UpdateCandidate)
            .Produces<CandidateResponse>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/delete/{id:int}", DeleteCandidate)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetCandidates(AppDbContext db, CancellationToken ct)
    {
        var candidates = await db.Candidates.AsNoTracking().ToListAsync(ct);
        return Results.Ok(candidates.Select(CandidateResponse.From));
    }

    private static async Task<IResult> CreateCandidate(
        CreateCandidateRequest payload,
        AppDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        new RequestValidator()
            .MinLength("firstName", payload.FirstName, 3, "firstName must be at least 3 characters")
            .MinLength("lastName", payload.LastName, 3, "lastName must be at least 3 characters")
            .Email("email", payload.Email, "email must be a valid email address")
            .ThrowIfInvalid();

        var candidate = new Candidate
        {
            FirstName = payload.FirstName,
            LastName = payload.LastName,
            Email = payload.Email,
            Phone = payload.Phone,
            CreatedAt = time.GetUtcNow().UtcDateTime,
        };

        db.Candidates.Add(candidate);
        await db.SaveChangesAsync(ct);

        return Results.Json(CandidateResponse.From(candidate), statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> GetCandidate(int id, AppDbContext db, CancellationToken ct)
    {
        var candidate = await db.Candidates.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Candidate not found");

        return Results.Ok(CandidateResponse.From(candidate));
    }

    private static async Task<IResult> UpdateCandidate(
        int id,
        UpdateCandidateRequest payload,
        AppDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        new RequestValidator()
            .MinLength("firstName", payload.FirstName, 3, "firstName must be at least 3 characters")
            .MinLength("lastName", payload.LastName, 3, "lastName must be at least 3 characters")
            .Email("email", payload.Email, "email must be a valid email address")
            .ThrowIfInvalid();

        var candidate = await db.Candidates.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Candidate not found");

        if (payload.FirstName is not null)
        {
            candidate.FirstName = payload.FirstName;
        }

        if (payload.LastName is not null)
        {
            candidate.LastName = payload.LastName;
        }

        if (payload.Phone is not null)
        {
            candidate.Phone = payload.Phone;
        }

        if (payload.Email is not null)
        {
            candidate.Email = payload.Email;
        }

        candidate.UpdatedAt = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);

        return Results.Ok(CandidateResponse.From(candidate));
    }

    private static async Task<IResult> DeleteCandidate(int id, AppDbContext db, TimeProvider time, CancellationToken ct)
    {
        var candidate = await db.Candidates.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Candidate not found");

        candidate.DeletedAt = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }
}
