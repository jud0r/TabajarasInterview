using Microsoft.EntityFrameworkCore;
using TabajarasInterview.Api.Data;

namespace TabajarasInterview.Api.Configuration;

/// <summary>
/// Dependency injection registration for the SQL Server database.
/// </summary>
public static class DatabaseExtensions
{
    /// <summary>
    /// Registers <see cref="AppDbContext"/> using the <c>DefaultConnection</c> connection string,
    /// with transient-failure retries enabled.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The same service collection.</returns>
    /// <exception cref="InvalidOperationException">The <c>DefaultConnection</c> connection string is missing.</exception>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

        return services;
    }
}
