namespace TabajarasInterview.Api.Models;

/// <summary>
/// Contract for entities that are soft deleted instead of being physically removed.
/// </summary>
public interface ISoftDeleteEntity
{
    /// <summary>Soft-delete timestamp (UTC). <see langword="null"/> while the row is active.</summary>
    DateTime? DeletedAt { get; set; }
}
