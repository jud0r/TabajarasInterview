using TabajarasInterview.Web.DTOs;
using TabajarasInterview.Web.Models;

namespace TabajarasInterview.Web.Services.Api
{
    public class UserApiService(IHttpClientFactory factory, ApiResponseParserService parser): IUserApiService
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
    }
}
