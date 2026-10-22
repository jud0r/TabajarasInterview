namespace TabajarasInterview.Web.DTOs;

/// <summary>
/// Candidate application returned by <c>GET /api/candidateApplications/byPosition/{id}</c>
/// (maps to the <c>candidateApplications</c> table). Property names map to the API's
/// camelCase JSON through <see cref="Services.Api.ApiResponseParserService"/>.
/// Every field except <see cref="Id"/> is optional so the UI degrades gracefully when
/// the API omits data.
/// </summary>
public sealed class ApplicationResponse
{
    public int Id { get; set; }

    public int CandidateId { get; set; }

    public int PositionId { get; set; }

    public string? Status { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public double? FinalScore { get; set; }

    public string? FinalComments { get; set; }
}
