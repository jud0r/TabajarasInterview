using TabajarasInterview.Web.DTOs;
using TabajarasInterview.Web.Models;

namespace TabajarasInterview.Web.Services.Api
{
    public class AuthApiService(IHttpClientFactory factory, ApiResponseParserService parser, AuthorizedHttpClientFactory authorizedFactory) : IAuthApiService
    {
        public async Task<ApiResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
        {
            var client = factory.CreateClient("rust-api");
            var response = await client.PostAsJsonAsync("api/auth/login", request, ct);
            return await parser.ParseAsync<LoginResponse>(response, ct);
        }

        public async Task<ApiResult<LoginResponse>> RefreshAsync(string refreshToken, CancellationToken ct = default)
        {
            var client = factory.CreateClient("rust-api");
            var response = await client.PostAsJsonAsync("api/auth/refresh", new { RefreshToken = refreshToken }, ct);
            return await parser.ParseAsync<LoginResponse>(response, ct);
        }

        public async Task<ApiResult> LogoutAsync(string refreshToken, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }
}
