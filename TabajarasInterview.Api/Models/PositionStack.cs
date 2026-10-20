namespace TabajarasInterview.Api.Models;

/// <summary>
/// Join entity between positions and stacks. Maps to <c>position_stacks</c> (composite key).
/// </summary>
public class PositionStack
{
    /// <summary>Id of the <see cref="Position"/> (part of the key).</summary>
    public int PositionId { get; set; }

    /// <summary>Id of the <see cref="Stack"/> (part of the key).</summary>
    public int StackId { get; set; }

    /// <summary>Creation timestamp (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Last update timestamp (UTC).</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Soft-delete timestamp (UTC).</summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>Related position.</summary>
    public Position? Position { get; set; }

    /// <summary>Related stack.</summary>
    public Stack? Stack { get; set; }
}
