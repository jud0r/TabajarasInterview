namespace TabajarasInterview.Api.Models;

/// <summary>
/// Technology stack. Maps to the <c>stacks</c> table.
/// </summary>
public class Stack : SoftDeleteEntity
{
    /// <summary>Unique stack name.</summary>
    public required string Name { get; set; }

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Join rows linking this stack to positions.</summary>
    public ICollection<PositionStack> PositionStacks { get; set; } = [];

    /// <summary>Questions belonging to this stack.</summary>
    public ICollection<Question> Questions { get; set; } = [];

    /// <summary>Positions using this stack (skip navigation).</summary>
    public ICollection<Position> Positions { get; set; } = [];
}
