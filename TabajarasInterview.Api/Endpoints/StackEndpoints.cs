using Microsoft.EntityFrameworkCore;
using TabajarasInterview.Api.Common;
using TabajarasInterview.Api.Data;
using TabajarasInterview.Api.Dtos;
using TabajarasInterview.Api.Models;

namespace TabajarasInterview.Api.Endpoints;

public sealed record CreateStackRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
}

public sealed record UpdateStackRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
}

/// <summary>Port of Rust <c>handlers/stacks.rs</c>, mounted at <c>/api/stacks</c>.</summary>
public static class StackEndpoints
{
    public static IEndpointRouteBuilder MapStackEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stacks")
            .WithTags("stacks")
            .RequireAuthorization()
            .AddEndpointFilter<ApiErrorFilter>();

        group.MapGet("/get_all", GetStacks).Produces<List<StackResponse>>();

        group.MapPost("/create", CreateStack)
            .Produces<StackResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapGet("/get/{id:int}", GetStack)
            .Produces<StackResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/update/{id:int}", UpdateStack)
            .Produces<StackResponse>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/delete/{id:int}", DeleteStack)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<Stack> RequireStackAsync(AppDbContext db, int id, bool track, CancellationToken ct)
    {
        var query = track ? db.Stacks : db.Stacks.AsNoTracking();
        return await query.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Stack not found");
    }

    private static async Task<IResult> GetStacks(AppDbContext db, CancellationToken ct)
    {
        var stacks = await db.Stacks.AsNoTracking().ToListAsync(ct);
        return Results.Ok(stacks.Select(StackResponse.From));
    }

    private static async Task<IResult> CreateStack(
        CreateStackRequest payload,
        AppDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        new RequestValidator()
            .MinLength("name", payload.Name, 1, "name must not be empty")
            .ThrowIfInvalid();

        var stack = new Stack
        {
            Name = payload.Name,
            Description = payload.Description,
            CreatedAt = time.GetUtcNow().UtcDateTime,
        };

        db.Stacks.Add(stack);
        await db.SaveChangesAsync(ct);

        return Results.Json(StackResponse.From(stack), statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> GetStack(int id, AppDbContext db, CancellationToken ct) =>
        Results.Ok(StackResponse.From(await RequireStackAsync(db, id, track: false, ct)));

    private static async Task<IResult> UpdateStack(
        int id,
        UpdateStackRequest payload,
        AppDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        new RequestValidator()
            .MinLength("name", payload.Name, 1, "name must not be empty")
            .ThrowIfInvalid();

        var stack = await RequireStackAsync(db, id, track: true, ct);

        if (payload.Name is not null)
        {
            stack.Name = payload.Name;
        }

        if (payload.Description is not null)
        {
            stack.Description = payload.Description;
        }

        stack.UpdatedAt = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);

        return Results.Ok(StackResponse.From(stack));
    }

    private static async Task<IResult> DeleteStack(int id, AppDbContext db, TimeProvider time, CancellationToken ct)
    {
        var stack = await RequireStackAsync(db, id, track: true, ct);

        stack.DeletedAt = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }
}
