namespace TabajarasInterview.Api.Models;

/// <summary>
/// Open job position. Maps to the <c>positions</c> table.
/// </summary>
public class Position : SoftDeleteEntity
{
    /// <summary>Id of the <see cref="User"/> that created the position.</summary>
    public int CreatedBy { get; set; }

    /// <summary>Position title.</summary>
    public required string Title { get; set; }

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Position status (free-form string, max 15 characters).</summary>
    public required string Status { get; set; }

    /// <summary>Work model: remote, hybrid or onsite (max 15 characters).</summary>
    public required string WorkModel { get; set; }

    /// <summary>Creator of the position.</summary>
    public User? CreatedByUser { get; set; }

    /// <summary>Applications received for this position.</summary>
    public ICollection<CandidateApplication> CandidateApplications { get; set; } = [];

    /// <summary>Join rows linking this position to locations.</summary>
    public ICollection<PositionLocation> PositionLocations { get; set; } = [];

    /// <summary>Join rows linking this position to stacks.</summary>
    public ICollection<PositionStack> PositionStacks { get; set; } = [];

    /// <summary>Locations of this position (skip navigation).</summary>
    public ICollection<Location> Locations { get; set; } = [];

    /// <summary>Stacks required by this position (skip navigation).</summary>
    public ICollection<Stack> Stacks { get; set; } = [];
}
