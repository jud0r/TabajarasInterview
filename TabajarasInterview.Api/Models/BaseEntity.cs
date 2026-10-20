namespace TabajarasInterview.Api.Models;

/// <summary>
/// Base class for entities with an identity key and audit timestamps.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Creation timestamp (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Last update timestamp (UTC).</summary>
    public DateTime? UpdatedAt { get; set; }
}
