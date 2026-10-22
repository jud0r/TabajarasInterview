using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TabajarasInterview.Api.Common;
using TabajarasInterview.Api.Data;
using TabajarasInterview.Api.Models;

namespace TabajarasInterview.Api.Endpoints;

public sealed record InterviewReviewerResponse(
    int Id,
    int InterviewId,
    int ReviewerId,
    string? ReviewScore,
    string? ReviewComments,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static InterviewReviewerResponse From(InterviewReviewer r) =>
        new(
            r.Id,
            r.InterviewId,
            r.ReviewerId,
            r.ReviewScore?.ToString(CultureInfo.InvariantCulture),
            r.ReviewComments,
            r.CreatedAt,
            r.UpdatedAt);
}

public sealed record CreateInterviewReviewerRequest
{
    public int InterviewId { get; init; }
    public int ReviewerId { get; init; }
    public string? ReviewScore { get; init; }
    public string? ReviewComments { get; init; }
}

public sealed record UpdateInterviewReviewerRequest
{
    public string? ReviewScore { get; init; }
    public string? ReviewComments { get; init; }
}

/// <summary>Port of Rust <c>handlers/interview_reviewers.rs</c>, mounted at <c>/api/interviewReviewers</c>.</summary>
public static class InterviewReviewerEndpoints
{
    public static IEndpointRouteBuilder MapInterviewReviewerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/interviewReviewers")
            .WithTags("interviewReviewers")
            .RequireAuthorization()
            .AddEndpointFilter<ApiErrorFilter>();

        group.MapGet("/getAll", GetAll).Produces<List<InterviewReviewerResponse>>();

        group.MapPost("/create", Create)
            .Produces<InterviewReviewerResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);

        group.MapGet("/get/{id:int}", Get)
            .Produces<InterviewReviewerResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/update/{id:int}", Update)
            .Produces<InterviewReviewerResponse>()
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
            : throw new ApiException(StatusCodes.Status400BadRequest, "reviewScore must be a valid decimal");

    private static async Task<InterviewReviewer> RequireAsync(AppDbContext db, int id, bool track, CancellationToken ct)
    {
        var query = track ? db.InterviewReviewers : db.InterviewReviewers.AsNoTracking();
        return await query.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Interview reviewer not found");
    }

    private static async Task<IResult> GetAll(AppDbContext db, CancellationToken ct)
    {
        var items = await db.InterviewReviewers.AsNoTracking().ToListAsync(ct);
        return Results.Ok(items.Select(InterviewReviewerResponse.From).ToList());
    }

    private static async Task<IResult> Create(
        CreateInterviewReviewerRequest payload,
        AppDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        if (await db.InterviewReviewers.AnyAsync(
                r => r.InterviewId == payload.InterviewId && r.ReviewerId == payload.ReviewerId, ct))
        {
            throw new ApiException(StatusCodes.Status409Conflict, "Reviewer already assigned to this interview");
        }

        var reviewScore = payload.ReviewScore is null ? (decimal?)null : ParseScore(payload.ReviewScore);

        var item = new InterviewReviewer
        {
            InterviewId = payload.InterviewId,
            ReviewerId = payload.ReviewerId,
            ReviewScore = reviewScore,
            ReviewComments = payload.ReviewComments,
            CreatedAt = time.GetUtcNow().UtcDateTime,
        };

        db.InterviewReviewers.Add(item);
        await db.SaveChangesAsync(ct);

        return Results.Json(InterviewReviewerResponse.From(item), statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> Get(int id, AppDbContext db, CancellationToken ct) =>
        Results.Ok(InterviewReviewerResponse.From(await RequireAsync(db, id, track: false, ct)));

    private static async Task<IResult> Update(
        int id,
        UpdateInterviewReviewerRequest payload,
        AppDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        var item = await RequireAsync(db, id, track: true, ct);

        if (payload.ReviewScore is not null)
        {
            item.ReviewScore = ParseScore(payload.ReviewScore);
        }

        if (payload.ReviewComments is not null)
        {
            item.ReviewComments = payload.ReviewComments;
        }

        item.UpdatedAt = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);

        return Results.Ok(InterviewReviewerResponse.From(item));
    }

    private static async Task<IResult> Delete(int id, AppDbContext db, TimeProvider time, CancellationToken ct)
    {
        var item = await RequireAsync(db, id, track: true, ct);

        item.DeletedAt = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }
}
