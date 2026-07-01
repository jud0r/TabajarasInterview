using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MudBlazor.Services;
using TabajarasInterview.Web;
using TabajarasInterview.Web.Services.Api;
using TabajarasInterview.Web.Services.Auth;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Authenticate server requests by validating the JWT stored in the access-token cookie,
// so endpoint authorization for [Authorize] pages reflects the real auth state.
// The cookie scheme is kept only to issue the login-redirect challenge.
var jwtSecret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret))
{
    throw new InvalidOperationException(
        "Jwt:Secret is not configured. Set it via the 'Jwt__Secret' environment variable " +
        "(or another configuration source) so it matches the API's signing key. " +
        "It must never be committed to source control.");
}

// HS256 signing keys must be at least 128 bits (16 bytes). Microsoft.IdentityModel rejects
// shorter keys with IDX10653 while building the signature provider, which surfaces as a
// misleading IDX10517 "signature validation failed" (kid missing) and silently rejects every
// token even though the secret is otherwise correct. Fail fast with an actionable message.
var jwtKeyBytes = Encoding.UTF8.GetBytes(jwtSecret);
if (jwtKeyBytes.Length < 16)
{
    throw new InvalidOperationException(
        $"Jwt:Secret is too short for HS256: {jwtKeyBytes.Length} bytes ({jwtKeyBytes.Length * 8} bits). " +
        "Microsoft.IdentityModel requires at least 16 bytes (128 bits); RFC 7518 recommends 32 bytes " +
        "(256 bits). Use the same value as the Rust API's SECRET.");
}

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(jwtKeyBytes),

            // The Rust API issues tokens without 'iss'/'aud' claims. Issuer and audience
            // validation default to true, which would reject every token, so disable them.
            // Trust is established by the symmetric signing key and the token lifetime.
            ValidateIssuer = false,
            ValidateAudience = false,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,

            //NameClaimType = JwtRegisteredClaimNames.UniqueName
        };

        // The token is delivered in the access-token cookie, not the Authorization header.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // HttpRequest.Cookies already URL-decodes the stored value, so the JWT is used
                // as-is. Decoding again here would mangle the base64url signature segment and
                // make every token fail signature validation (IDX10517).
                var token = context.Request.Cookies["tabajaras_access_token"];
                if (!string.IsNullOrEmpty(token))
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            },
            // Surface the exact reason a token was rejected (e.g. signature/secret mismatch,
            // expired lifetime) so authentication problems are visible in the logs.
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtBearerAuth");
                logger.LogWarning(context.Exception, "JWT authentication failed: {Reason}", context.Exception.Message);
                return Task.CompletedTask;
            }
        };
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddHttpContextAccessor();
builder.Services.AddMudServices();

builder.Services.AddScoped<ApiResponseParserService>();
builder.Services.AddHttpClient("rust-api", client =>
{
    client.BaseAddress = new("http://rust-api");
});

builder.Services.AddScoped<IUserApiService, UserApiService>();
builder.Services.AddScoped<IAuthApiService, AuthApiService>();
builder.Services.AddScoped<IDashboardApiService, DashboardApiService>();
builder.Services.AddScoped<AuthorizedHttpClientFactory>();
builder.Services.AddScoped<CookieService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProviderService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
