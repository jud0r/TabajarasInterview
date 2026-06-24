using TabajarasInterview.Web.DTOs;
using TabajarasInterview.Web.Models;

namespace TabajarasInterview.Web.Services.Api
{
    public interface IAuthApiService
    {
        Task<ApiResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
        Task<ApiResult<LoginResponse>> RefreshAsync(string refreshToken, CancellationToken ct = default);
        Task<ApiResult> LogoutAsync(string refreshToken, CancellationToken ct = default);
    }
}
