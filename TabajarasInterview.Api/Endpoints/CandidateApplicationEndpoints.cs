using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TabajarasInterview.Api.Common;
using TabajarasInterview.Api.Data;
using TabajarasInterview.Api.Models;

namespace TabajarasInterview.Api.Endpoints;

/// <summary>JSON uses camelCase ("offerExtended"); the database stores the PascalCase name.</summary>
public enum CandidateApplicationStatus
{
    Applied,
    Screening,
    Interviewing,
    OfferExtended,
    Hired,
    Rejected,
    Withdrawn,
}

public sealed record CandidateApplicationResponse(
    int Id,
    int CandidateId,
    int PositionId,
    CandidateApplicationStatus Status,
    DateTime StartedAt,
    DateTime? FinishedAt,
    string? FinalScore,
    string? FinalComments,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static CandidateApplicationResponse From(CandidateApplication a)
    {
        if (!Enum.TryParse<CandidateApplicationStatus>(a.Status, ignoreCase: false, out var status)
            || !Enum.IsDefined(status))
        {
            throw new ApiException(StatusCodes.Status500InternalServerError, "Unknown candidate application status");
        }

        return new CandidateApplicationResponse(
            a.Id,
            a.CandidateId,
            a.PositionId,
            status,
            a.StartedAt,
            a.FinishedAt,
            a.FinalScore?.ToString(CultureInfo.InvariantCulture),
            a.FinalComments,
            a.CreatedAt,
            a.UpdatedAt);
    }
}

public sealed record CreateCandidateApplicationRequest
{
    public int CandidateId { get; init; }
    public int PositionId { get; init; }
    public CandidateApplicationStatus Status { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? FinishedAt { get; init; }
    public string? FinalScore { get; init; }
    public string? FinalComments { get; init; }
}

public sealed record UpdateCandidateApplicationRequest
{
    public CandidateApplicationStatus? Status { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? FinishedAt { get; init; }
    public string? FinalScore { get; init; }
    public string? FinalComments { get; init; }
}

/// <summary>Port of Rust <c>handlers/candidateApplications.rs</c>, mounted at <c>/api/candidateApplications</c>.</summary>
public static class CandidateApplicationEndpoints
{
    public static IEndpointRouteBuilder MapCandidateApplicationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/candidateApplications")
            .WithTags("candidateApplications")
            .RequireAuthorization()
            .AddEndpointFilter<ApiErrorFilter>();

        group.MapGet("/getAll", GetAll).Produces<List<CandidateApplicationResponse>>();
        group.MapGet("/byCandidate/{candidateId:int}", GetByCandidate).Produces<List<CandidateApplicationResponse>>();
        group.MapGet("/byPosition/{positionId:int}", GetByPosition).Produces<List<CandidateApplicationResponse>>();

        group.MapPost("/create", Create)
            .Produces<CandidateApplicationResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);

        group.MapGet("/get/{id:int}", Get)
            .Produces<CandidateApplicationResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/update/{id:int}", Update)
            .Produces<CandidateApplicationResponse>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/delete/{id:int}", Delete)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static decimal ParseScore(string value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var score)
            ? score
            : throw new ApiException(StatusCodes.Status400BadRequest, "finalScore must be a valid decimal");

    private static async Task<CandidateApplication> RequireAsync(AppDbContext db, int id, bool track, CancellationToken ct)
    {
        var query = track ? db.CandidateApplications : db.CandidateApplications.AsNoTracking();
        return await query.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Candidate application not found");
    }

    private static async Task<IResult> GetAll(AppDbContext db, CancellationToken ct)
    {
        var items = await db.CandidateApplications.AsNoTracking().ToListAsync(ct);
        return Results.Ok(items.Select(CandidateApplicationResponse.From).ToList());
    }

    private static async Task<IResult> GetByCandidate(int candidateId, AppDbContext db, CancellationToken ct)
    {
        var items = await db.CandidateApplications.AsNoTracking()
            .Where(a => a.CandidateId == candidateId).ToListAsync(ct);
        return Results.Ok(items.Select(CandidateApplicationResponse.From).ToList());
    }

    private static async Task<IResult> GetByPosition(int positionId, AppDbContext db, CancellationToken ct)
    {
        var items = await db.CandidateApplications.AsNoTracking()
            .Where(a => a.PositionId == positionId).ToListAsync(ct);
        return Results.Ok(items.Select(CandidateApplicationResponse.From).ToList());
    }

    private static async Task<IResult> Create(
        CreateCandidateApplicationRequest payload,
        AppDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        if (await db.CandidateApplications.AnyAsync(
                a => a.CandidateId == payload.CandidateId && a.PositionId == payload.PositionId, ct))
        {
            throw new ApiException(StatusCodes.Status409Conflict, "Candidate already applied to this position");
        }

        var finalScore = payload.FinalScore is null ? (decimal?)null : ParseScore(payload.FinalScore);
        var now = time.GetUtcNow().UtcDateTime;

        var application = new CandidateApplication
        {
            CandidateId = payload.CandidateId,
            PositionId = payload.PositionId,
            Status = payload.Status.ToString(),
            StartedAt = payload.StartedAt ?? now,
            FinishedAt = payload.FinishedAt,
            FinalScore = finalScore,
            FinalComments = payload.FinalComments,
            CreatedAt = now,
        };

        db.CandidateApplications.Add(application);
        await db.SaveChangesAsync(ct);

        return Results.Json(CandidateApplicationResponse.From(application), statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> Get(int id, AppDbContext db, CancellationToken ct) =>
        Results.Ok(CandidateApplicationResponse.From(await RequireAsync(db, id, track: false, ct)));

    private static async Task<IResult> Update(
        int id,
        UpdateCandidateApplicationRequest payload,
        AppDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        var application = await RequireAsync(db, id, track: true, ct);

        if (payload.Status is { } status)
        {
            application.Status = status.ToString();
        }

        if (payload.StartedAt is { } startedAt)
        {
            application.StartedAt = startedAt;
        }

        if (payload.FinishedAt is { } finishedAt)
        {
            application.FinishedAt = finishedAt;
        }

        if (payload.FinalScore is not null)
        {
            application.FinalScore = ParseScore(payload.FinalScore);
        }

        if (payload.FinalComments is not null)
        {
            application.FinalComments = payload.FinalComments;
        }

        application.UpdatedAt = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);

        return Results.Ok(CandidateApplicationResponse.From(application));
    }

    private static async Task<IResult> Delete(int id, AppDbContext db, TimeProvider time, CancellationToken ct)
    {
        var application = await RequireAsync(db, id, track: true, ct);

        application.DeletedAt = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }
}
