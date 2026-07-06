using TabajarasInterview.Web.DTOs;
using TabajarasInterview.Web.Models;

namespace TabajarasInterview.Web.Services.Api
{
    /// <summary>
    /// Contract for the positions API client. All calls target the rust-api
    /// over <c>HttpClient</c>; the Blazor app never touches the database.
    /// </summary>
    public interface IPositionApiService
    {
        /// <summary>Lists positions (<c>GET /api/positions/get_all</c>).</summary>
        Task<ApiResult<List<PositionResponse>>> GetPositionsAsync(CancellationToken ct = default);

        /// <summary>Fetches a single position (<c>GET /api/positions/get/{id}</c>).</summary>
        Task<ApiResult<PositionResponse>> GetPositionByIdAsync(int id, CancellationToken ct = default);

        /// <summary>Creates a position (<c>POST /api/positions/create</c>).</summary>
        Task<ApiResult<PositionResponse>> CreatePositionAsync(CreatePositionRequest request, CancellationToken ct = default);

        /// <summary>Updates a position (<c>PUT /api/positions/update/{id}</c>).</summary>
        Task<ApiResult<PositionResponse>> UpdatePositionAsync(int id, UpdatePositionRequest request, CancellationToken ct = default);

        /// <summary>Soft-deletes a position (<c>DELETE /api/positions/delete/{id}</c>).</summary>
        Task<ApiResult> DeletePositionAsync(int id, CancellationToken ct = default);

        /// <summary>
        /// Composes candidate/interview statistics for a position from
        /// <c>GET /api/candidate_applications/by_position/{id}</c> and
        /// <c>GET /api/interviews/get_all</c> (no single rust-api endpoint returns this today).
        /// </summary>
        Task<ApiResult<PositionStatsResponse>> GetPositionStatsAsync(int id, CancellationToken ct = default);

        /// <summary>
        /// Assigns a stack to a position (<c>POST /api/positions/get/{id}/stacks/{stackId}</c>).
        /// Returns the position's full stack list. There is no bulk "set stacks" endpoint,
        /// so callers must assign/remove one stack at a time.
        /// </summary>
        Task<ApiResult<List<StackResponse>>> AssignStackAsync(int positionId, int stackId, CancellationToken ct = default);

        /// <summary>
        /// Removes a stack from a position (<c>DELETE /api/positions/get/{id}/stacks/{stackId}</c>).
        /// Returns the position's remaining stack list.
        /// </summary>
        Task<ApiResult<List<StackResponse>>> RemoveStackAsync(int positionId, int stackId, CancellationToken ct = default);
    }
}
