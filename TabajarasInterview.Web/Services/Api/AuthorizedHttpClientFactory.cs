using System.Net.Http.Headers;
using TabajarasInterview.Web.Services.Auth;

namespace TabajarasInterview.Web.Services.Api
{
    public class AuthorizedHttpClientFactory(IHttpClientFactory factory, AuthService authService, IServiceProvider serviceProvider)
    {
        public async Task<HttpClient> CreateClientAsync(string name = "rust-api")
        {
            var client = factory.CreateClient(name);
            var token = await authService.GetTokenAsync();

            if (string.IsNullOrWhiteSpace(token))
            {
                var refreshToken = await authService.GetRefreshTokenAsync();
                if (!string.IsNullOrWhiteSpace(refreshToken))
                {
                    var authApi = serviceProvider.GetRequiredService<IAuthApiService>();
                    var result = await authApi.RefreshAsync(refreshToken);
                    if (result is { Success: true, Data: not null })
                    {
                        await authService.LoginAsync(result.Data);
                        token = result.Data.AccessToken;
                    }
                    else
                    {
                        await authService.LogoutAsync();
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }
    }
}
