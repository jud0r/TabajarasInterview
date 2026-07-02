using TabajarasInterview.Web.DTOs;
using TabajarasInterview.Web.Models;

namespace TabajarasInterview.Web.Services.Api
{
    /// <summary>
    /// Contract for the stacks API client. All calls target the rust-api
    /// over <c>HttpClient</c>; the Blazor app never touches the database.
    /// </summary>
    public interface IStackApiService
    {
        /// <summary>Lists stacks (<c>GET /api/stacks/get_all</c>).</summary>
        Task<ApiResult<List<StackResponse>>> GetStacksAsync(CancellationToken ct = default);

        /// <summary>Fetches a single stack (<c>GET /api/stacks/get/{id}</c>).</summary>
        Task<ApiResult<StackResponse>> GetStackByIdAsync(int id, CancellationToken ct = default);

        /// <summary>Creates a stack (<c>POST /api/stacks/create</c>).</summary>
        Task<ApiResult<StackResponse>> CreateStackAsync(CreateStackRequest request, CancellationToken ct = default);

        /// <summary>Updates a stack (<c>PUT /api/stacks/update/{id}</c>).</summary>
        Task<ApiResult<StackResponse>> UpdateStackAsync(int id, UpdateStackRequest request, CancellationToken ct = default);

        /// <summary>Soft-deletes a stack (<c>DELETE /api/stacks/delete/{id}</c>).</summary>
        Task<ApiResult> DeleteStackAsync(int id, CancellationToken ct = default);
    }
}
