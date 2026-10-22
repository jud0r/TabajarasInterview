using Microsoft.EntityFrameworkCore;
using TabajarasInterview.Api.Data;

namespace TabajarasInterview.Api.Configuration;

public static class DatabaseStartupExtensions
{
    /// <summary>Blocks until the database is reachable, retrying every 2 seconds (Rust connect_with_retry).</summary>
    public static async Task WaitForDatabaseAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        while (true)
        {
            try
            {
                await using var scope = app.Services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                if (await db.Database.CanConnectAsync(cancellationToken))
                {
                    app.Logger.LogInformation("✅ Connected to DB");
                    return;
                }

                app.Logger.LogWarning("⏳ Waiting for DB");
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                app.Logger.LogWarning("⏳ Waiting for DB: {Message}", e.Message);
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }
    }
}
