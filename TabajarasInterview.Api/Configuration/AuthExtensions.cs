using TabajarasInterview.Api.Auth;

namespace TabajarasInterview.Api.Configuration;

public static class AuthExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => !string.IsNullOrEmpty(o.Secret), "Jwt:Secret not set")
            .ValidateOnStart();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<JwtTokenService>();

        services.AddAuthentication(ApiBearerHandler.SchemeName)
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, ApiBearerHandler>(
                ApiBearerHandler.SchemeName, null);
        services.AddAuthorization();

        return services;
    }
}
