using Microsoft.EntityFrameworkCore;
using TabajarasInterview.Api.Auth;
using TabajarasInterview.Api.Common;
using TabajarasInterview.Api.Data;
using TabajarasInterview.Api.Dtos;
using TabajarasInterview.Api.Models;

namespace TabajarasInterview.Api.Endpoints;

/// <summary>Port of Rust <c>handlers/positions.rs</c>, mounted at <c>/api/positions</c>.</summary>
public static class PositionEndpoints
{
    public static IEndpointRouteBuilder MapPositionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/positions")
            .WithTags("positions")
            .RequireAuthorization()
            .AddEndpointFilter<ApiErrorFilter>();

        group.MapGet("/get_all", GetPositions).Produces<List<PositionResponse>>();

        group.MapPost("/create", CreatePosition)
            .Produces<PositionResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapGet("/get/{id:int}", GetPosition)
            .Produces<PositionResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/update/{id:int}", UpdatePosition)
            .Produces<PositionResponse>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/delete/{id:int}", DeletePosition)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/get/{id:int}/candidates", GetPositionCandidates)
            .Produces<List<CandidateResponse>>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/get/{id:int}/stacks", GetPositionStacks)
            .Produces<List<StackResponse>>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/get/{id:int}/stacks/{stackId:int}", AssignPositionStack)
            .Produces<List<StackResponse>>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapDelete("/get/{id:int}/stacks/{stackId:int}", RemovePositionStack)
            .Produces<List<StackResponse>>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/get/{id:int}/locations", GetPositionLocations)
            .Produces<List<LocationResponse>>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/get/{id:int}/locations/{locationId:int}", AssignPositionLocation)
            .Produces<List<LocationResponse>>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapDelete("/get/{id:int}/locations/{locationId:int}", RemovePositionLocation)
            .Produces<List<LocationResponse>>()
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    /// <summary>Loads the locations linked to a position through the join table (active links only).</summary>
    private static async Task<List<LocationResponse>> LoadLocationsAsync(AppDbContext db, int positionId, CancellationToken ct)
    {
        var locationIds = db.PositionLocations
            .Where(l => l.PositionId == positionId && l.DeletedAt == null)
            .Select(l => l.LocationId);

        var locations = await db.Locations.AsNoTracking()
            .Where(l => locationIds.Contains(l.Id))
            .ToListAsync(ct);

        return locations.Select(LocationResponse.From).ToList();
    }

    /// <summary>Loads the active stacks linked to a position through the join table.</summary>
    private static async Task<List<StackResponse>> LoadStacksAsync(AppDbContext db, int positionId, CancellationToken ct)
    {
        var stackIds = db.PositionStacks
            .Where(l => l.PositionId == positionId && l.DeletedAt == null)
            .Select(l => l.StackId);

        var stacks = await db.Stacks.AsNoTracking()
            .Where(s => stackIds.Contains(s.Id))
            .ToListAsync(ct);

        return stacks.Select(StackResponse.From).ToList();
    }

    private static async Task<Position> RequirePositionAsync(AppDbContext db, int id, bool track, CancellationToken ct)
    {
        var query = track ? db.Positions : db.Positions.AsNoTracking();
        return await query.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Position not found");
    }

    private static async Task<PositionResponse> WithRelationsAsync(AppDbContext db, Position position, CancellationToken ct) =>
        PositionResponse.From(position) with
        {
            Stacks = await LoadStacksAsync(db, position.Id, ct),
            Locations = await LoadLocationsAsync(db, position.Id, ct),
        };

    private static async Task<IResult> GetPositions(AppDbContext db, CancellationToken ct)
    {
        var positions = await db.Positions.AsNoTracking().ToListAsync(ct);

        var response = new List<PositionResponse>(positions.Count);
        foreach (var position in positions)
        {
            response.Add(await WithRelationsAsync(db, position, ct));
        }

        return Results.Ok(response);
    }

    private static async Task<IResult> CreatePosition(
        CreatePositionRequest payload,
        HttpContext http,
        AppDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        new RequestValidator()
            .MinLength("title", payload.Title, 3, "title must be at least 3 characters")
            .ThrowIfInvalid();

        var position = new Position
        {
            Title = payload.Title,
            Description = payload.Description,
            Status = payload.Status.ToDbValue(),
            WorkModel = payload.WorkModel.ToDbValue(),
            CreatedBy = http.User.GetAuthUser().Id,
            CreatedAt = time.GetUtcNow().UtcDateTime,
        };

        db.Positions.Add(position);
        await db.SaveChangesAsync(ct);

        return Results.Json(PositionResponse.From(position), statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> GetPosition(int id, AppDbContext db, CancellationToken ct)
    {
        var position = await RequirePositionAsync(db, id, track: false, ct);
        return Results.Ok(await WithRelationsAsync(db, position, ct));
    }

    private static async Task<IResult> UpdatePosition(
        int id,
        UpdatePositionRequest payload,
        AppDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        new RequestValidator()
            .MinLength("title", payload.Title, 3, "title must be at least 3 characters")
            .ThrowIfInvalid();

        var position = await RequirePositionAsync(db, id, track: true, ct);

        if (payload.Title is not null)
        {
            position.Title = payload.Title;
        }

        if (payload.Description is not null)
        {
            position.Description = payload.Description;
        }

        if (payload.Status is { } status)
        {
            position.Status = status.ToDbValue();
        }

        if (payload.WorkModel is { } workModel)
        {
            position.WorkModel = workModel.ToDbValue();
        }

        position.UpdatedAt = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);

        return Results.Ok(await WithRelationsAsync(db, position, ct));
    }

    private static async Task<IResult> DeletePosition(int id, AppDbContext db, TimeProvider time, CancellationToken ct)
    {
        var position = await RequirePositionAsync(db, id, track: true, ct);

        position.DeletedAt = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }

    private static async Task<IResult> GetPositionCandidates(int id, AppDbContext db, CancellationToken ct)
    {
        await RequirePositionAsync(db, id, track: false, ct);

        var candidateIds = db.CandidateApplications
            .Where(a => a.PositionId == id)
            .Select(a => a.CandidateId);

        var candidates = await db.Candidates.AsNoTracking()
            .Where(c => candidateIds.Contains(c.Id))
            .ToListAsync(ct);

        return Results.Ok(candidates.Select(CandidateResponse.From));
    }

    private static async Task<IResult> GetPositionStacks(int id, AppDbContext db, CancellationToken ct)
    {
        await RequirePositionAsync(db, id, track: false, ct);
        return Results.Ok(await LoadStacksAsync(db, id, ct));
    }

    private static async Task<IResult> AssignPositionStack(
        int id,
        int stackId,
        AppDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        await RequirePositionAsync(db, id, track: false, ct);

        if (!await db.Stacks.AnyAsync(s => s.Id == stackId, ct))
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Stack not found");
        }

        // Soft-deleted links still exist physically: revive them instead of colliding on the composite key.
        var existing = await db.PositionStacks
            .FirstOrDefaultAsync(l => l.PositionId == id && l.StackId == stackId, ct);

        var now = time.GetUtcNow().UtcDateTime;

        if (existing is { DeletedAt: null })
        {
            throw new ApiException(StatusCodes.Status409Conflict, "Stack already assigned to this position");
        }

        if (existing is not null)
        {
            existing.DeletedAt = null;
            existing.UpdatedAt = now;
        }
        else
        {
            db.PositionStacks.Add(new PositionStack { PositionId = id, StackId = stackId, CreatedAt = now });
        }

        await db.SaveChangesAsync(ct);

        return Results.Json(await LoadStacksAsync(db, id, ct), statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> RemovePositionStack(
        int id,
        int stackId,
        AppDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        var link = await db.PositionStacks
            .FirstOrDefaultAsync(l => l.PositionId == id && l.StackId == stackId && l.DeletedAt == null, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Position stack link not found");

        link.DeletedAt = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);

        return Results.Ok(await LoadStacksAsync(db, id, ct));
    }

    private static async Task<IResult> GetPositionLocations(int id, AppDbContext db, CancellationToken ct)
    {
        await RequirePositionAsync(db, id, track: false, ct);
        return Results.Ok(await LoadLocationsAsync(db, id, ct));
    }

    private static async Task<IResult> AssignPositionLocation(
        int id,
        int locationId,
        AppDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        await RequirePositionAsync(db, id, track: false, ct);

        if (!await db.Locations.AnyAsync(l => l.Id == locationId, ct))
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Location not found");
        }

        var existing = await db.PositionLocations
            .FirstOrDefaultAsync(l => l.PositionId == id && l.LocationId == locationId, ct);

        var now = time.GetUtcNow().UtcDateTime;

        if (existing is { DeletedAt: null })
        {
            throw new ApiException(StatusCodes.Status409Conflict, "Location already assigned to this position");
        }

        if (existing is not null)
        {
            existing.DeletedAt = null;
            existing.UpdatedAt = now;
        }
        else
        {
            db.PositionLocations.Add(new PositionLocation { PositionId = id, LocationId = locationId, CreatedAt = now });
        }

        await db.SaveChangesAsync(ct);

        return Results.Json(await LoadLocationsAsync(db, id, ct), statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> RemovePositionLocation(
        int id,
        int locationId,
        AppDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        var link = await db.PositionLocations
            .FirstOrDefaultAsync(l => l.PositionId == id && l.LocationId == locationId && l.DeletedAt == null, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Position location link not found");

        link.DeletedAt = time.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);

        return Results.Ok(await LoadLocationsAsync(db, id, ct));
    }
}
