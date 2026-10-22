using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Authorization;
using TabajarasInterview.Web.DTOs;

namespace TabajarasInterview.Web.Services.Auth
{

    public class AuthService(CookieService cookies, AuthenticationStateProvider authProvider, HttpClient http)
    {
        private const string TokenKey = "tabajarasAccessToken";
        private const string RefreshTokenKey = "tabajarasRefreshToken";
        private const int RefreshTokenExpirationInSeconds = 60 * 60 * 24 * 30; // 30 days

        private string? _accessToken;
        private string? _refreshToken;

        public async Task<bool> LoginAsync(LoginResponse login)
        {
            if (string.IsNullOrWhiteSpace(login?.AccessToken))
                return false;

            _accessToken = login.AccessToken;
            _refreshToken = login.RefreshToken;

            await cookies.SetAsync(TokenKey, login.AccessToken, login.ExpiresIn > 0 ? login.ExpiresIn : 3600);
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
            _accessToken = null;
            _refreshToken = null;
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
            return _accessToken ?? await cookies.GetAsync(TokenKey);
        }

        public async Task<string?> GetRefreshTokenAsync()
        {
            return _refreshToken ?? await cookies.GetAsync(RefreshTokenKey);
        }

        /// <summary>
        /// Refreshes the cached auth-state identity (name/email) without re-authenticating.
        /// Call after a profile update so components like the header menu reflect the new name.
        /// </summary>
        public async Task RefreshUserAsync(UserResponse user)
        {
            if (authProvider is CustomAuthenticationStateProviderService custom)
            {
                await custom.MarkUserAsAuthenticated(user);
            }
        }
    }
}
