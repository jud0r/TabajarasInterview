namespace TabajarasInterview.Api.Models;

/// <summary>
/// Position location (city). Maps to the <c>locations</c> table.
/// </summary>
public class Location
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>City name.</summary>
    public required string City { get; set; }

    /// <summary>Join rows linking this location to positions.</summary>
    public ICollection<PositionLocation> PositionLocations { get; set; } = [];

    /// <summary>Positions in this location (skip navigation).</summary>
    public ICollection<Position> Positions { get; set; } = [];
}
