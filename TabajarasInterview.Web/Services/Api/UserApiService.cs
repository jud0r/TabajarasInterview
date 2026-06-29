using TabajarasInterview.Web.DTOs;
using TabajarasInterview.Web.Models;

namespace TabajarasInterview.Web.Services.Api
{
    public class UserApiService(
        IHttpClientFactory factory,
        AuthorizedHttpClientFactory authorizedFactory,
        ApiResponseParserService parser) : IUserApiService
    {
        private const string BaseUrl = "rust-api";

        public async Task<ApiResult<UserResponse>> CreateAsync(RegisterRequest request, CancellationToken ct = default)
        {
            var client = factory.CreateClient(BaseUrl);
            var payload = new
            {
                first_name = request.FirstName,
                last_name = request.LastName,
                email = request.Email,
                password = request.Password
            };

            var response = await client.PostAsJsonAsync("api/users/create", payload, ct);
            return await parser.ParseAsync<UserResponse>(response, ct);
        }

        public async Task<ApiResult<UserResponse>> GetCurrentAsync(CancellationToken ct = default)
        {
            var client = await authorizedFactory.CreateClientAsync(BaseUrl);

            var response = await client.GetAsync("api/users/get", ct);
            return await parser.ParseAsync<UserResponse>(response, ct);
        }

        public async Task<ApiResult<UserResponse>> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default)
        {
            var client = await authorizedFactory.CreateClientAsync(BaseUrl);
            var payload = new
            {
                first_name = request.FirstName,
                last_name = request.LastName
            };

            var response = await client.PutAsJsonAsync("api/users/update", payload, ct);
            return await parser.ParseAsync<UserResponse>(response, ct);
        }

        public async Task<ApiResult> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default)
        {
            var client = await authorizedFactory.CreateClientAsync(BaseUrl);
            var payload = new
            {
                current_password = request.CurrentPassword,
                new_password = request.NewPassword
            };

            var response = await client.PutAsJsonAsync("api/users/password", payload, ct);
            return await parser.ParseAsync(response, ct);
        }
    }
}
