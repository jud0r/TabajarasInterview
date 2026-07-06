namespace TabajarasInterview.Web.DTOs;

/// <summary>
/// Candidate/interview statistics for a single position, composed client-side
/// (by <see cref="Services.Api.PositionApiService"/>) from
/// <c>GET /api/candidate_applications/by_position/{id}</c> and
/// <c>GET /api/interviews/get_all</c>, since the rust-api has no single
/// endpoint that returns these counts directly.
/// </summary>
public sealed class PositionStatsResponse
{
    /// <summary>Number of (non-deleted) candidate applications submitted for this position.</summary>
    public int ApplicationCount { get; set; }

    /// <summary>Number of interviews held across all of this position's applications.</summary>
    public int InterviewCount { get; set; }
}
