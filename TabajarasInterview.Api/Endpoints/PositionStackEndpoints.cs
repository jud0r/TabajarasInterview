using Microsoft.EntityFrameworkCore;
using TabajarasInterview.Api.Common;
using TabajarasInterview.Api.Data;
using TabajarasInterview.Api.Models;

namespace TabajarasInterview.Api.Endpoints;

public sealed record PositionStackResponse(
    int PositionId,
    int StackId,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static PositionStackResponse From(PositionStack s) =>
        new(s.PositionId, s.StackId, s.CreatedAt, s.UpdatedAt);
}

public sealed record CreatePositionStackRequest
{
    public int PositionId { get; init; }
    public int StackId { get; init; }
}

/// <summary>Port of Rust <c>handlers/position_stacks.rs</c>, mounted at <c>/api/positionStacks</c>.</summary>
public static class PositionStackEndpoints
{
    public static IEndpointRouteBuilder MapPositionStackEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/positionStacks")
            .WithTags("positionStacks")
            .RequireAuthorization()
            .AddEndpointFilter<ApiErrorFilter>();

        group.MapGet("/getAll", GetAll).Produces<List<PositionStackResponse>>();

        group.MapPost("/create", Create)
            .Produces<PositionStackResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);

        group.MapGet("/get/{positionId:int}/{stackId:int}", Get)
            .Produces<PositionStackResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/delete/{positionId:int}/{stackId:int}", Delete)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<PositionStack> RequireActiveAsync(
        AppDbContext db, int positionId, int stackId, bool track, CancellationToken ct)
    {
        var query = track ? db.PositionStacks : db.PositionStacks.AsNoTracking();
        return await query.FirstOrDefaultAsync(
                s => s.PositionId == positionId && s.StackId == stackId && s.DeletedAt == null, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Position stack not found");
    }

    private static async Task<IResult> GetAll(AppDbContext db, CancellationToken ct)
    {
        var items = await db.PositionStacks.AsNoTracking().Where(s => s.DeletedAt == null).ToListAsync(ct);
        return Results.Ok(items.Select(PositionStackResponse.From).ToList());
    }

    private static async Task<IResult> Create(
        CreatePositionStackRequest payload,
        AppDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        if (await db.PositionStacks.AnyAsync(
                s => s.PositionId == payload.PositionId && s.StackId == payload.StackId && s.DeletedAt == null, ct))
        {
            throw new ApiException(StatusCodes.Status409Conflict, "Stack already linked to this position");
        }

        var item = new PositionStack
        {
            PositionId = payload.PositionId,
            StackId = payload.StackId,
            CreatedAt = time.GetUtcNow().UtcDateTime,
        };

        db.PositionStacks.Add(item);
        await db.SaveChangesAsync(ct);

        return Results.Json(PositionStackResponse.From(item), statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> Get(int positionId, int stackId, AppDbContext db, CancellationToken ct) =>
        Results.Ok(PositionStackResponse.From(await RequireActiveAsync(db, positionId, stackId, track: false, ct)));

    private static async Task<IResult> Delete(
        int positionId, int stackId, AppDbContext db, TimeProvider time, CancellationToken ct)
    {
        var item = await RequireActiveAsync(db, positionId, stackId, track: true, ct);

        item.DeletedAt = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }
}
