using TabajarasInterview.Web.DTOs;
using TabajarasInterview.Web.DTOs.Candidate;
using TabajarasInterview.Web.Models;

namespace TabajarasInterview.Web.Services.Api
{
    /// <summary>
    /// Contract for the candidate-applications API client. All calls target the
    /// rust-api over <c>HttpClient</c>; the Blazor app never touches the database.
    /// </summary>
    public interface ICandidateApiService
    {
        /// <summary>Lists candidates (<c>GET /api/candidates</c>).</summary>
        Task<ApiResult<List<CandidateResponse>>> GetCandidatesAsync(CancellationToken ct = default);

        /// <summary>Fetches a single candidate (<c>GET /api/candidates/{id}</c>).</summary>
        Task<ApiResult<CandidateResponse>> GetCandidateByIdAsync(int id, CancellationToken ct = default);

        /// <summary>Creates a candidate (<c>POST /api/candidates</c>).</summary>
        Task<ApiResult<CandidateResponse>> CreateCandidateAsync(CandidateRequest request, CancellationToken ct = default);

        /// <summary>Updates a candidate (<c>PUT /api/candidates/{id}</c>).</summary>
        Task<ApiResult<CandidateResponse>> UpdateCandidateAsync(int id, CandidateRequest request, CancellationToken ct = default);

        /// <summary>Soft-deletes a candidate (<c>DELETE /api/candidates/{id}</c>).</summary>
        Task<ApiResult> DeleteCandidateAsync(int id, CancellationToken ct = default);

        /// <summary>
        /// Lists the candidate's applications/positions with nested interviews, reviewers,
        /// questions and stacks (<c>GET /api/candidates/{id}/positions</c>). The API is
        /// expected to back this endpoint with the <c>get_candidate_positions(candidate_id)</c>
        /// data source.
        /// </summary>
        Task<ApiResult<List<CandidateApplicationResponse>>> GetCandidatePositionsAsync(int candidateId, CancellationToken ct = default);

        /// <summary>
        /// Fetches the candidate's recruitment timeline
        /// (<c>GET /api/candidates/{id}/timeline</c>).
        /// </summary>
        Task<ApiResult<List<TimelineEvent>>> GetCandidateTimelineAsync(int candidateId, CancellationToken ct = default);
    }
}
