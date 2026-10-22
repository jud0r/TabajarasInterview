namespace TabajarasInterview.Api.Models;

/// <summary>
/// Interview candidate. Maps to the <c>candidates</c> table.
/// </summary>
public class Candidate : SoftDeleteEntity
{
    /// <summary>First name.</summary>
    public required string FirstName { get; set; }

    /// <summary>Last name.</summary>
    public required string LastName { get; set; }

    /// <summary>E-mail address (not unique in the original schema).</summary>
    public required string Email { get; set; }

    /// <summary>Optional phone number.</summary>
    public string? Phone { get; set; }

    /// <summary>Applications submitted by this candidate.</summary>
    public ICollection<CandidateApplication> CandidateApplications { get; set; } = [];
}
