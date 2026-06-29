using TabajarasInterview.Web.DTOs;
using TabajarasInterview.Web.Models;

namespace TabajarasInterview.Web.Services.Api
{
    public interface IUserApiService
    {
        Task<ApiResult<UserResponse>> CreateAsync(RegisterRequest request, CancellationToken ct = default);

        /// <summary>Fetches the currently authenticated user (<c>GET /api/users/get</c>).</summary>
        Task<ApiResult<UserResponse>> GetCurrentAsync(CancellationToken ct = default);

        /// <summary>Updates the current user's profile (<c>PUT /api/users/update</c>).</summary>
        Task<ApiResult<UserResponse>> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default);

        /// <summary>Changes the current user's password (<c>PUT /api/users/password</c>).</summary>
        Task<ApiResult> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default);
    }
}
