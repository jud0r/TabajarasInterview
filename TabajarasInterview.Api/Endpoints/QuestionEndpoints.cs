using Microsoft.EntityFrameworkCore;
using TabajarasInterview.Api.Common;
using TabajarasInterview.Api.Data;
using TabajarasInterview.Api.Models;

namespace TabajarasInterview.Api.Endpoints;

public sealed record QuestionResponse(
    int Id,
    int StackId,
    string Question,
    string? AcceptableAnswer,
    string? Level,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static QuestionResponse From(Question q) =>
        new(q.Id, q.StackId, q.Text, q.AcceptableAnswer, q.Level, q.CreatedAt, q.UpdatedAt);
}

public sealed record CreateQuestionRequest
{
    public int StackId { get; init; }
    public required string Question { get; init; }
    public string? AcceptableAnswer { get; init; }
    public string? Level { get; init; }
}

public sealed record UpdateQuestionRequest
{
    public int? StackId { get; init; }
    public string? Question { get; init; }
    public string? AcceptableAnswer { get; init; }
    public string? Level { get; init; }
}

/// <summary>Port of Rust <c>handlers/questions.rs</c>, mounted at <c>/api/questions</c>.</summary>
public static class QuestionEndpoints
{
    public static IEndpointRouteBuilder MapQuestionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/questions")
            .WithTags("questions")
            .RequireAuthorization()
            .AddEndpointFilter<ApiErrorFilter>();

        group.MapGet("/getAll", GetQuestions).Produces<List<QuestionResponse>>();
        group.MapGet("/byStack/{stackId:int}", GetQuestionsByStack).Produces<List<QuestionResponse>>();

        group.MapPost("/create", CreateQuestion)
            .Produces<QuestionResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapGet("/get/{id:int}", GetQuestion)
            .Produces<QuestionResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/update/{id:int}", UpdateQuestion)
            .Produces<QuestionResponse>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/delete/{id:int}", DeleteQuestion)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<Question> RequireQuestionAsync(AppDbContext db, int id, bool track, CancellationToken ct)
    {
        var query = track ? db.Questions : db.Questions.AsNoTracking();
        return await query.FirstOrDefaultAsync(q => q.Id == id, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Question not found");
    }

    private static async Task<IResult> GetQuestions(AppDbContext db, CancellationToken ct)
    {
        var questions = await db.Questions.AsNoTracking().ToListAsync(ct);
        return Results.Ok(questions.Select(QuestionResponse.From));
    }

    private static async Task<IResult> GetQuestionsByStack(int stackId, AppDbContext db, CancellationToken ct)
    {
        var questions = await db.Questions.AsNoTracking().Where(q => q.StackId == stackId).ToListAsync(ct);
        return Results.Ok(questions.Select(QuestionResponse.From));
    }

    private static async Task<IResult> CreateQuestion(
        CreateQuestionRequest payload,
        AppDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        new RequestValidator()
            .MinLength("question", payload.Question, 1, "question must not be empty")
            .ThrowIfInvalid();

        var question = new Question
        {
            StackId = payload.StackId,
            Text = payload.Question,
            AcceptableAnswer = payload.AcceptableAnswer,
            Level = payload.Level,
            CreatedAt = time.GetUtcNow().UtcDateTime,
        };

        db.Questions.Add(question);
        await db.SaveChangesAsync(ct);

        return Results.Json(QuestionResponse.From(question), statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> GetQuestion(int id, AppDbContext db, CancellationToken ct) =>
        Results.Ok(QuestionResponse.From(await RequireQuestionAsync(db, id, track: false, ct)));

    private static async Task<IResult> UpdateQuestion(
        int id,
        UpdateQuestionRequest payload,
        AppDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        new RequestValidator()
            .MinLength("question", payload.Question, 1, "question must not be empty")
            .ThrowIfInvalid();

        var question = await RequireQuestionAsync(db, id, track: true, ct);

        if (payload.StackId is { } stackId)
        {
            question.StackId = stackId;
        }

        if (payload.Question is not null)
        {
            question.Text = payload.Question;
        }

        if (payload.AcceptableAnswer is not null)
        {
            question.AcceptableAnswer = payload.AcceptableAnswer;
        }

        if (payload.Level is not null)
        {
            question.Level = payload.Level;
        }

        question.UpdatedAt = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);

        return Results.Ok(QuestionResponse.From(question));
    }

    private static async Task<IResult> DeleteQuestion(int id, AppDbContext db, TimeProvider time, CancellationToken ct)
    {
        var question = await RequireQuestionAsync(db, id, track: true, ct);

        question.DeletedAt = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }
}
