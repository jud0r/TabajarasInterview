using TabajarasInterview.Web.DTOs;
using TabajarasInterview.Web.Models;

namespace TabajarasInterview.Web.Services.Api
{
    /// <summary>
    /// Contract for the candidate-applications API client. All calls target the
    /// rust-api over <c>HttpClient</c>; the Blazor app never touches the database.
    /// </summary>
    public interface IApplicationApiService
    {
        /// <summary>Lists the applications for a position (<c>GET /api/candidate_applications/by_position/{id}</c>).</summary>
        Task<ApiResult<List<ApplicationResponse>>> GetApplicationsByPositionAsync(int positionId, CancellationToken ct = default);
    }
}
