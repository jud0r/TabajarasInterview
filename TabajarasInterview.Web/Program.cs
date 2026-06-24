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

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ApiResponseParserService>();
builder.Services.AddScoped<IAuthApiService, AuthApiService>();
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

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
