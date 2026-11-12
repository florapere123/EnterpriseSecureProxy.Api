using EnterpriseSecureProxy.Application.Dtos;
using EnterpriseSecureProxy.Application.Interfaces;
using EnterpriseSecureProxy.Infrastructure.Persistence;
using EnterpriseSecureProxy.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EnterpriseSecureProxy.Tests;

public sealed class ProxyOperationAdminServiceIntegrationTests
{
    [Fact]
    public async Task UpdateOperationAsync_PersistsChangeAndConfigurationHistory()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ProxyDbContext>()
            .UseSqlite(connection)
            .Options;

        var operationId = TelemetryGatewaySeedData.SensorReadingOpId;

        await using (var seed = new ProxyDbContext(options))
        {
            await seed.Database.EnsureCreatedAsync();
            await seed.Database.ExecuteSqlRawAsync(
                "INSERT INTO Destinations (Id, Name, BaseUrl, IsActive, TimeoutMilliseconds, SettingsJson, RowVersion, CreatedAt, UpdatedAt) " +
                "VALUES ({0}, {1}, {2}, 1, 15000, NULL, X'01020304', {3}, {3})",
                TelemetryGatewaySeedData.RadarDestinationId,
                "telemetry.radar-feed",
                "https://telemetry.internal:8443",
                DateTime.UtcNow);

            await seed.Database.ExecuteSqlRawAsync(
                "INSERT INTO ProxyOperations (Id, OperationKey, DestinationId, HttpMethod, RelativePath, IsActive, MaxRequestSize, TimeoutMilliseconds, RequiresEncryption, AllowUnknownParameters, AllowHttpClientRetryForDelete, AllowedRoles, RowVersion) " +
                "VALUES ({0}, {1}, {2}, 'GET', {3}, 1, 8192, NULL, 0, 0, 0, 'SensorClient', X'AABBCCDD')",
                operationId,
                "sensor.reading.get",
                TelemetryGatewaySeedData.RadarDestinationId,
                "/api/v1/sensors/reading");
        }

        var factory = new SingleConnectionDbContextFactory(options);
        var admin = new ProxyOperationAdminService(
            factory,
            new StubCacheInvalidator(),
            new OptimisticConcurrencyUpdater(),
            NullLogger<ProxyOperationAdminService>.Instance);

        var view = await admin.GetOperationAsync(operationId);
        Assert.NotNull(view);

        var update = await admin.UpdateOperationAsync(operationId, new UpdateProxyOperationRequest
        {
            RowVersion = view!.RowVersion,
            RelativePath = "/api/v2/sensors/reading"
        });

        Assert.True(update.Success, update.Message);
        Assert.False(update.ConcurrencyConflict);

        var after = await admin.GetOperationAsync(operationId);
        Assert.Equal("/api/v2/sensors/reading", after!.RelativePath);

        await using var verify = new ProxyDbContext(options);
        var historyCount = await verify.ConfigurationHistory
            .CountAsync(h => h.EntityType == "ProxyOperation" && h.EntityId == operationId);
        Assert.Equal(1, historyCount);
    }

    private sealed class SingleConnectionDbContextFactory : IDbContextFactory<ProxyDbContext>
    {
        private readonly DbContextOptions<ProxyDbContext> _options;

        public SingleConnectionDbContextFactory(DbContextOptions<ProxyDbContext> options) => _options = options;

        public ProxyDbContext CreateDbContext() => new(_options);

        public Task<ProxyDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new ProxyDbContext(_options));
    }

    private sealed class StubCacheInvalidator : IConfigurationCacheInvalidator
    {
        public void EvictOperation(string operationKey) { }
        public void EvictDestination(string destinationName) { }
        public void EvictAllOperations() { }
    }
}
