using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using TabajarasInterview.Web.DTOs;

namespace TabajarasInterview.Web.Services.Auth
{

    public class CustomAuthenticationStateProviderService(CookieService cookies,
        IHttpContextAccessor httpContextAccessor) : AuthenticationStateProvider
    {
        private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());
        private const string TokenCookie = "tabajaras_access_token";

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            string? token = null;

            // During prerendering, read from HttpContext; during interactive, use JS
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
            if (string.IsNullOrWhiteSpace(token))
                return new AuthenticationState(_anonymous);

            var identity = new ClaimsIdentity(new[]
            {
                new Claim("access_token", token)
            }, "jwt");

            var user = new ClaimsPrincipal(identity);

            return new AuthenticationState(user);
        }

        public Task MarkUserAsAuthenticated(UserResponse user)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("user_id", user.Id.ToString())
            }, "jwt");

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
