using TabajarasInterview.Web.DTOs;
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
    }
}
