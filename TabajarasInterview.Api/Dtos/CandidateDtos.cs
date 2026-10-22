using TabajarasInterview.Api.Models;

namespace TabajarasInterview.Api.Dtos;

/// <summary>Rust <c>CandidateResponse</c> (candidates.rs).</summary>
public sealed record CandidateResponse(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static CandidateResponse From(Candidate candidate) =>
        new(
            candidate.Id,
            candidate.FirstName,
            candidate.LastName,
            candidate.Email,
            candidate.Phone,
            candidate.CreatedAt,
            candidate.UpdatedAt);
}

public sealed record CreateCandidateRequest
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public string? Phone { get; init; }
}

public sealed record UpdateCandidateRequest
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
}
