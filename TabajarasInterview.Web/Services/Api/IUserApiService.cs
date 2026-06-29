using TabajarasInterview.Web.DTOs;
using TabajarasInterview.Web.Models;

namespace TabajarasInterview.Web.Services.Api
{
    public interface IUserApiService
    {
        Task<ApiResult<UserResponse>> CreateAsync(RegisterRequest request, CancellationToken ct = default);
    }
}
