using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using TabajarasInterview.Web.DTOs;

namespace TabajarasInterview.Web.Services.Auth
{

    public class CustomAuthenticationStateProviderService(CookieService cookies,
        IHttpContextAccessor httpContextAccessor,
        JwtTokenValidator validator) : AuthenticationStateProvider
    {
        private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());
        private const string TokenCookie = "tabajaras_access_token";

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            string? token;

            // During prerendering the cookie is available on HttpContext; during the
            // interactive circuit there is no HttpContext, so fall back to JS interop.
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext is not null)
            {
                token = httpContext.Request.Cookies[TokenCookie];
                if (token is not null)
                    token = Uri.UnescapeDataString(token);
            }
            else
            {
                token = await cookies.GetAsync(TokenCookie);
            }

            // Validate the token (signature, lifetime, token_type) instead of trusting the
            // mere presence of a cookie, so the UI state matches HttpContext.User on the server.
            var principal = await validator.ValidateAsync(token);

            return new AuthenticationState(principal ?? _anonymous);
        }

        public Task MarkUserAsAuthenticated(UserResponse user)
        {
            // Mirror the claim set produced from a validated token so the auth state is
            // identical whether it came from MarkUserAsAuthenticated or a page reload.
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("token_type", JwtTokenValidator.AccessTokenType),
                new Claim("full_name", user.FullName)
            }, JwtTokenValidator.AuthenticationType, ClaimTypes.Name, ClaimTypes.Role);

            var principal = new ClaimsPrincipal(identity);

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));

            return Task.CompletedTask;
        }

        public Task MarkUserAsLoggedOut()
        {
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
            return Task.CompletedTask;
        }
    }
}
