using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TabajarasInterview.Api.Common;
using TabajarasInterview.Api.Data;
using TabajarasInterview.Api.Models;

namespace TabajarasInterview.Api.Endpoints;

/// <summary>JSON uses snake_case; the database stores the PascalCase name.</summary>
public enum InterviewType
{
    Screening,
    Technical,
    Behavioral,
    Final,
}

/// <summary>JSON uses snake_case ("in_progress", "no_show"); the database stores the PascalCase name.</summary>
public enum InterviewStatus
{
    Scheduled,
    InProgress,
    Completed,
    Cancelled,
    NoShow,
    Rescheduled,
}

public sealed record InterviewResponse(
    int Id,
    int CandidateApplicationId,
    int InterviewerId,
    string Name,
    InterviewType InterviewType,
    InterviewStatus Status,
    string? Score,
    string? Comments,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static InterviewResponse From(Interview i)
    {
        if (!Enum.TryParse<InterviewType>(i.InterviewType, false, out var type) || !Enum.IsDefined(type))
        {
            throw new ApiException(StatusCodes.Status500InternalServerError, "Unknown interview type");
        }

        if (!Enum.TryParse<InterviewStatus>(i.Status, false, out var status) || !Enum.IsDefined(status))
        {
            throw new ApiException(StatusCodes.Status500InternalServerError, "Unknown interview status");
        }

        return new InterviewResponse(
            i.Id,
            i.CandidateApplicationId,
            i.InterviewerId,
            i.Name,
            type,
            status,
            i.Score?.ToString(CultureInfo.InvariantCulture),
            i.Comments,
            i.CreatedAt,
            i.UpdatedAt);
    }
}

public sealed record CreateInterviewRequest
{
    public int CandidateApplicationId { get; init; }
    public int InterviewerId { get; init; }
    public required string Name { get; init; }
    public InterviewType InterviewType { get; init; }
    public InterviewStatus Status { get; init; }
    public string? Score { get; init; }
    public string? Comments { get; init; }
}

public sealed record UpdateInterviewRequest
{
    public string? Name { get; init; }
    public InterviewType? InterviewType { get; init; }
    public InterviewStatus? Status { get; init; }
    public string? Score { get; init; }
    public string? Comments { get; init; }
}

/// <summary>Port of Rust <c>handlers/interviews.rs</c>, mounted at <c>/api/interviews</c>.</summary>
public static class InterviewEndpoints
{
    public static IEndpointRouteBuilder MapInterviewEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/interviews")
            .WithTags("interviews")
            .RequireAuthorization()
            .AddEndpointFilter<ApiErrorFilter>();

        group.MapGet("/get_all", GetAll).Produces<List<InterviewResponse>>();

        group.MapPost("/create", Create)
            .Produces<InterviewResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapGet("/get/{id:int}", Get)
            .Produces<InterviewResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/update/{id:int}", Update)
            .Produces<InterviewResponse>()
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
            : throw new ApiException(StatusCodes.Status400BadRequest, "score must be a valid decimal");

    private static async Task<Interview> RequireAsync(AppDbContext db, int id, bool track, CancellationToken ct)
    {
        var query = track ? db.Interviews : db.Interviews.AsNoTracking();
        return await query.FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Interview not found");
    }

    private static async Task<IResult> GetAll(AppDbContext db, CancellationToken ct)
    {
        var items = await db.Interviews.AsNoTracking().ToListAsync(ct);
        return Results.Ok(items.Select(InterviewResponse.From).ToList());
    }

    private static async Task<IResult> Create(
        CreateInterviewRequest payload,
        AppDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        new RequestValidator()
            .MinLength("name", payload.Name, 1, "name must not be empty")
            .ThrowIfInvalid();

        var score = payload.Score is null ? (decimal?)null : ParseScore(payload.Score);

        var interview = new Interview
        {
            CandidateApplicationId = payload.CandidateApplicationId,
            InterviewerId = payload.InterviewerId,
            Name = payload.Name,
            InterviewType = payload.InterviewType.ToString(),
            Status = payload.Status.ToString(),
            Score = score,
            Comments = payload.Comments,
            CreatedAt = time.GetUtcNow().UtcDateTime,
        };

        db.Interviews.Add(interview);
        await db.SaveChangesAsync(ct);

        return Results.Json(InterviewResponse.From(interview), statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> Get(int id, AppDbContext db, CancellationToken ct) =>
        Results.Ok(InterviewResponse.From(await RequireAsync(db, id, track: false, ct)));

    private static async Task<IResult> Update(
        int id,
        UpdateInterviewRequest payload,
        AppDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        new RequestValidator()
            .MinLength("name", payload.Name, 1, "name must not be empty")
            .ThrowIfInvalid();

        var interview = await RequireAsync(db, id, track: true, ct);

        if (payload.Name is not null)
        {
            interview.Name = payload.Name;
        }

        if (payload.InterviewType is { } type)
        {
            interview.InterviewType = type.ToString();
        }

        if (payload.Status is { } status)
        {
            interview.Status = status.ToString();
        }

        if (payload.Score is not null)
        {
            interview.Score = ParseScore(payload.Score);
        }

        if (payload.Comments is not null)
        {
            interview.Comments = payload.Comments;
        }

        interview.UpdatedAt = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);

        return Results.Ok(InterviewResponse.From(interview));
    }

    private static async Task<IResult> Delete(int id, AppDbContext db, TimeProvider time, CancellationToken ct)
    {
        var interview = await RequireAsync(db, id, track: true, ct);

        interview.DeletedAt = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }
}
