using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EnterpriseSecureProxy.Infrastructure.Persistence;

/// <summary>Runs EF migrations and seeds starter configuration at startup for the SQL data source.</summary>
public static class SeedDataExtensions
{
    public static async Task MigrateAndSeedProxyConfigurationAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ProxyDbContext>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ProxyDbContext>>();

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        try
        {
            await db.Database.MigrateAsync(cancellationToken);

            if (await db.ProxyOperations.AnyAsync(cancellationToken))
            {
                logger.LogInformation("Proxy configuration database already populated; skipping startup seed.");
                return;
            }

            logger.LogInformation("Seeding Telemetry Gateway proxy configuration.");
            await TelemetryGatewaySeedData.InsertIfEmptyAsync(db, cancellationToken);
            logger.LogInformation("Telemetry Gateway seed completed.");
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }
}
