namespace TabajarasInterview.Web.DTOs;

/// <summary>
/// Candidate returned by the rust-api (mirrors the API's <c>CandidateResponse</c>
/// and the <c>candidates</c> table). Property names map to the API's snake_case
/// JSON through <see cref="Services.Api.ApiResponseParserService"/>.
/// </summary>
public sealed class CandidateResponse
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    /// <summary>Display helper combining <see cref="FirstName"/> and <see cref="LastName"/>.</summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    /// <summary>Maps to <c>created_at</c>.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Maps to <c>updated_at</c> (null until the candidate is edited).</summary>
    public DateTime? UpdatedAt { get; set; }
}
