using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;
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
            // During prerendering/SSR the JWT bearer handler has already validated the
            // access-token cookie and populated HttpContext.User, so reuse that identity.
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext is not null)
            {
                if (httpContext.User.Identity?.IsAuthenticated != true)
                    return new AuthenticationState(_anonymous);

                var cookieToken = httpContext.Request.Cookies[TokenCookie];
                cookieToken = cookieToken is not null ? Uri.UnescapeDataString(cookieToken) : string.Empty;

                return new AuthenticationState(BuildPrincipal(httpContext.User.Claims, cookieToken));
            }

            // During interactive rendering there is no HttpContext; read the cookie via JS
            // and rebuild the identity from the JWT's claims.
            var token = await cookies.GetAsync(TokenCookie);
            if (string.IsNullOrWhiteSpace(token) || !TryReadClaims(token, out var claims))
                return new AuthenticationState(_anonymous);

            return new AuthenticationState(BuildPrincipal(claims, token));
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

        // Maps the JWT claims onto the same claim types produced by MarkUserAsAuthenticated so
        // an authenticated user looks identical whether they just logged in or reloaded the page.
        private static ClaimsPrincipal BuildPrincipal(IEnumerable<Claim> source, string accessToken)
        {
            var claims = new List<Claim>();

            if (!string.IsNullOrEmpty(accessToken))
                claims.Add(new Claim("access_token", accessToken));

            var name = source.FirstOrDefault(c => c.Type == "name")?.Value;
            if (!string.IsNullOrEmpty(name))
                claims.Add(new Claim(ClaimTypes.Name, name));

            var email = source.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value;
            if (!string.IsNullOrEmpty(email))
                claims.Add(new Claim(ClaimTypes.Email, email));

            var userId = source.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
            if (!string.IsNullOrEmpty(userId))
                claims.Add(new Claim("user_id", userId));

            return new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
        }

        private static bool TryReadClaims(string token, out IEnumerable<Claim> claims)
        {
            try
            {
                claims = new JsonWebToken(token).Claims;
                return true;
            }
            catch
            {
                claims = Array.Empty<Claim>();
                return false;
            }
        }
    }
}
