using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TabajarasInterview.Api.Common;
using TabajarasInterview.Api.Data;
using TabajarasInterview.Api.Models;

namespace TabajarasInterview.Api.Endpoints;

public sealed record InterviewQuestionResponse(
    int Id,
    int InterviewId,
    int QuestionId,
    int QuestionOrder,
    string? Score,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static InterviewQuestionResponse From(InterviewQuestion q) =>
        new(
            q.Id,
            q.InterviewId,
            q.QuestionId,
            q.QuestionOrder,
            q.Score?.ToString(CultureInfo.InvariantCulture),
            q.CreatedAt,
            q.UpdatedAt);
}

public sealed record CreateInterviewQuestionRequest
{
    public int InterviewId { get; init; }
    public int QuestionId { get; init; }
    public int QuestionOrder { get; init; }
    public string? Score { get; init; }
}

public sealed record UpdateInterviewQuestionRequest
{
    public int? QuestionOrder { get; init; }
    public string? Score { get; init; }
}

/// <summary>Port of Rust <c>handlers/interview_questions.rs</c>, mounted at <c>/api/interview_questions</c>.</summary>
public static class InterviewQuestionEndpoints
{
    public static IEndpointRouteBuilder MapInterviewQuestionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/interview_questions")
            .WithTags("interview_questions")
            .RequireAuthorization()
            .AddEndpointFilter<ApiErrorFilter>();

        group.MapGet("/get_all", GetAll).Produces<List<InterviewQuestionResponse>>();

        group.MapPost("/create", Create)
            .Produces<InterviewQuestionResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);

        group.MapGet("/get/{id:int}", Get)
            .Produces<InterviewQuestionResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/update/{id:int}", Update)
            .Produces<InterviewQuestionResponse>()
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

    private static async Task<InterviewQuestion> RequireAsync(AppDbContext db, int id, bool track, CancellationToken ct)
    {
        var query = track ? db.InterviewQuestions : db.InterviewQuestions.AsNoTracking();
        return await query.FirstOrDefaultAsync(q => q.Id == id, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Interview question not found");
    }

    private static async Task<IResult> GetAll(AppDbContext db, CancellationToken ct)
    {
        var items = await db.InterviewQuestions.AsNoTracking().ToListAsync(ct);
        return Results.Ok(items.Select(InterviewQuestionResponse.From).ToList());
    }

    private static async Task<IResult> Create(
        CreateInterviewQuestionRequest payload,
        AppDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        if (await db.InterviewQuestions.AnyAsync(
                q => q.InterviewId == payload.InterviewId && q.QuestionId == payload.QuestionId, ct))
        {
            throw new ApiException(StatusCodes.Status409Conflict, "Question already added to this interview");
        }

        var score = payload.Score is null ? (decimal?)null : ParseScore(payload.Score);

        var item = new InterviewQuestion
        {
            InterviewId = payload.InterviewId,
            QuestionId = payload.QuestionId,
            QuestionOrder = payload.QuestionOrder,
            Score = score,
            CreatedAt = time.GetUtcNow().UtcDateTime,
        };

        db.InterviewQuestions.Add(item);
        await db.SaveChangesAsync(ct);

        return Results.Json(InterviewQuestionResponse.From(item), statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> Get(int id, AppDbContext db, CancellationToken ct) =>
        Results.Ok(InterviewQuestionResponse.From(await RequireAsync(db, id, track: false, ct)));

    private static async Task<IResult> Update(
        int id,
        UpdateInterviewQuestionRequest payload,
        AppDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        var item = await RequireAsync(db, id, track: true, ct);

        if (payload.QuestionOrder is { } order)
        {
            item.QuestionOrder = order;
        }

        if (payload.Score is not null)
        {
            item.Score = ParseScore(payload.Score);
        }

        item.UpdatedAt = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);

        return Results.Ok(InterviewQuestionResponse.From(item));
    }

    private static async Task<IResult> Delete(int id, AppDbContext db, TimeProvider time, CancellationToken ct)
    {
        var item = await RequireAsync(db, id, track: true, ct);

        item.DeletedAt = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }
}
