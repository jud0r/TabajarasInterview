namespace TabajarasInterview.Api.Models;

/// <summary>
/// Base class for entities that support soft deletion.
/// </summary>
public abstract class SoftDeleteEntity : BaseEntity, ISoftDeleteEntity
{
    /// <inheritdoc />
    public DateTime? DeletedAt { get; set; }
}
