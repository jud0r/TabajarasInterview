using Xunit;
using Microsoft.AspNetCore.Http;
using TabajarasInterview.Web.DTOs;
using TabajarasInterview.Web.Services.Auth;

namespace TabajarasInterview.Web.Tests;

public class CustomAuthenticationStateProviderServiceTests
{
    private static CustomAuthenticationStateProviderService CreateProvider(HttpContext httpContext) =>
        new(cookies: null!, new HttpContextAccessor { HttpContext = httpContext });

    [Fact]
    public async Task GetAuthenticationStateAsync_UnauthenticatedHttpContext_IsAnonymous()
    {
        var provider = CreateProvider(new DefaultHttpContext());

        var state = await provider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task MarkUserAsAuthenticated_NotifiesSubscribersWithUserClaims()
    {
        var provider = CreateProvider(new DefaultHttpContext());
        Task<Microsoft.AspNetCore.Components.Authorization.AuthenticationState>? notified = null;
        provider.AuthenticationStateChanged += task => notified = task;

        await provider.MarkUserAsAuthenticated(new UserResponse { Id = 3, FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com" });

        var state = await notified!;
        Assert.True(state.User.Identity?.IsAuthenticated);
        Assert.Equal("Ada Lovelace", state.User.Identity!.Name);
    }

    [Fact]
    public async Task MarkUserAsLoggedOut_NotifiesAnonymousState()
    {
        var provider = CreateProvider(new DefaultHttpContext());
        Task<Microsoft.AspNetCore.Components.Authorization.AuthenticationState>? notified = null;
        provider.AuthenticationStateChanged += task => notified = task;

        await provider.MarkUserAsLoggedOut();

        var state = await notified!;
        Assert.False(state.User.Identity?.IsAuthenticated);
    }
}
