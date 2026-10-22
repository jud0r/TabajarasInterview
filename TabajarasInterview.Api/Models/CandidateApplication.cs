namespace TabajarasInterview.Api.Models;

/// <summary>
/// A candidate's application to a position. Maps to the <c>candidate_applications</c> table.
/// </summary>
public class CandidateApplication : SoftDeleteEntity
{
    /// <summary>Id of the <see cref="Candidate"/>.</summary>
    public int CandidateId { get; set; }

    /// <summary>Id of the <see cref="Position"/>.</summary>
    public int PositionId { get; set; }

    /// <summary>Application status (max 15 characters).</summary>
    public required string Status { get; set; }

    /// <summary>Start timestamp (UTC), defaulted by the database.</summary>
    public DateTime StartedAt { get; set; }

    /// <summary>Finish timestamp (UTC).</summary>
    public DateTime? FinishedAt { get; set; }

    /// <summary>Final score, <c>decimal(5,2)</c>.</summary>
    public decimal? FinalScore { get; set; }

    /// <summary>Final comments.</summary>
    public string? FinalComments { get; set; }

    /// <summary>Applying candidate.</summary>
    public Candidate? Candidate { get; set; }

    /// <summary>Target position.</summary>
    public Position? Position { get; set; }

    /// <summary>Interviews of this application.</summary>
    public ICollection<Interview> Interviews { get; set; } = [];
}
