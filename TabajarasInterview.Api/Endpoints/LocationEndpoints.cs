using Microsoft.EntityFrameworkCore;
using TabajarasInterview.Api.Common;
using TabajarasInterview.Api.Data;
using TabajarasInterview.Api.Dtos;

namespace TabajarasInterview.Api.Endpoints;

/// <summary>
/// Port of Rust <c>handlers/locations.rs</c> (<c>/api/locations</c>) and the
/// <c>GET /api/candidates/get/{id}/positions</c> endpoint from <c>candidates.rs</c>.
/// </summary>
public static class LocationEndpoints
{
    public static IEndpointRouteBuilder MapLocationEndpoints(this IEndpointRouteBuilder app)
    {
        var locations = app.MapGroup("/api/locations")
            .WithTags("locations")
            .RequireAuthorization()
            .AddEndpointFilter<ApiErrorFilter>();

        locations.MapGet("/getAll", async (AppDbContext db, CancellationToken ct) =>
        {
            var all = await db.Locations.AsNoTracking().ToListAsync(ct);
            return Results.Ok(all.Select(LocationResponse.From));
        }).Produces<List<LocationResponse>>();

        app.MapGet("/api/candidates/get/{id:int}/positions", GetCandidatePositions)
            .WithTags("candidates")
            .RequireAuthorization()
            .AddEndpointFilter<ApiErrorFilter>()
            .Produces<List<PositionResponse>>()
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetCandidatePositions(int id, AppDbContext db, CancellationToken ct)
    {
        if (!await db.Candidates.AnyAsync(c => c.Id == id, ct))
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Candidate not found");
        }

        var positionIds = db.CandidateApplications
            .Where(a => a.CandidateId == id)
            .Select(a => a.PositionId);

        var positions = await db.Positions.AsNoTracking()
            .Where(p => positionIds.Contains(p.Id))
            .ToListAsync(ct);

        return Results.Ok(positions.Select(PositionResponse.From).ToList());
    }
}
