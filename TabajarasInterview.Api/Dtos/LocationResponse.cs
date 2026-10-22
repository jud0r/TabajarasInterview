using TabajarasInterview.Api.Models;

namespace TabajarasInterview.Api.Dtos;

/// <summary>Rust <c>LocationResponse</c> (locations.rs).</summary>
public sealed record LocationResponse(int Id, string City)
{
    public static LocationResponse From(Location location) => new(location.Id, location.City);
}
