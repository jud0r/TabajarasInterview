using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
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

// Add MudBlazor services.
builder.Services.AddMudServices();

builder.Services.AddOutputCache();

builder.Services.AddHttpClient("rust-api", client =>
{
    client.BaseAddress = new("http://rust-api");
});

// AuthService depends on a bare HttpClient; map it to the named rust-api client so the
// dependency resolves and its Authorization header targets the API.
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("rust-api"));

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ApiResponseParserService>();
builder.Services.AddScoped<IAuthApiService, AuthApiService>();
builder.Services.AddScoped<AuthorizedHttpClientFactory>();
builder.Services.AddScoped<CookieService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProviderService>();

// Server-side validation of the rust-api access token. The same validator backs the
// Blazor AuthenticationStateProvider so HttpContext.User and the UI auth state agree.
var jwtValidator = new JwtTokenValidator(builder.Configuration);
builder.Services.AddSingleton(jwtValidator);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = jwtValidator.Parameters;
        options.Events = new JwtBearerEvents
        {
            // The token lives in a JS-set, URI-escaped cookie rather than the
            // Authorization header, so read it from the cookie on each request.
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue("tabajaras_access_token", out var token)
                    && !string.IsNullOrWhiteSpace(token))
                {
                    context.Token = Uri.UnescapeDataString(token);
                }

                return Task.CompletedTask;
            },
            // Enforce token_type == access and normalize claims so HttpContext.User
            // matches the principal produced by CustomAuthenticationStateProviderService.
            OnTokenValidated = context =>
            {
                var principal = context.Principal?.Identity is ClaimsIdentity identity
                    ? jwtValidator.BuildPrincipal(identity)
                    : null;

                if (principal is null)
                {
                    context.Fail("Invalid or non-access token.");
                }
                else
                {
                    context.Principal = principal;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

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

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
