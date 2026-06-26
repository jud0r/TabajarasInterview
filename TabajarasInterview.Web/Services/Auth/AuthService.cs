using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Authorization;
using TabajarasInterview.Web.DTOs;

namespace TabajarasInterview.Web.Services.Auth
{

    public class AuthService(CookieService cookies, AuthenticationStateProvider authProvider, HttpClient http)
    {
        private const string TokenKey = "tabajaras_access_token";
        private const string RefreshTokenKey = "tabajaras_refresh_token";
        private const int RefreshTokenExpirationInSeconds = 60 * 60 * 24 * 30; // 30 days

        public async Task<bool> LoginAsync(LoginResponse login)
        {
            if (string.IsNullOrWhiteSpace(login?.AccessToken))
                return false;

            await cookies.SetAsync(TokenKey, login.AccessToken, login.ExpiresIn);
            await cookies.SetAsync(RefreshTokenKey, login.RefreshToken, RefreshTokenExpirationInSeconds);

            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", login.AccessToken);

            if (authProvider is CustomAuthenticationStateProviderService custom)
            {
                await custom.MarkUserAsAuthenticated(login.User);
            }

            return true;
        }

        public async Task LogoutAsync()
        {
            await cookies.RemoveAsync(TokenKey);
            await cookies.RemoveAsync(RefreshTokenKey);

            http.DefaultRequestHeaders.Authorization = null;

            if (authProvider is CustomAuthenticationStateProviderService custom)
            {
                await custom.MarkUserAsLoggedOut();
            }
        }

        public async Task<string?> GetTokenAsync()
        {
            return await cookies.GetAsync(TokenKey);
        }

        public async Task<string?> GetRefreshTokenAsync()
        {
            return await cookies.GetAsync(RefreshTokenKey);
        }
    }
}
