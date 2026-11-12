using System.Net;
using System.Text.Json;
using EnterpriseSecureProxy.Application.Dtos;
using EnterpriseSecureProxy.Application.Interfaces;
using EnterpriseSecureProxy.Application.UseCases;
using EnterpriseSecureProxy.Domain.Entities;
using EnterpriseSecureProxy.Domain.Enums;
using EnterpriseSecureProxy.Infrastructure.Persistence;
using EnterpriseSecureProxy.Infrastructure.Persistence.Entities;
using EnterpriseSecureProxy.Infrastructure.Services;
using EnterpriseSecureProxy.Tests.Support;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace EnterpriseSecureProxy.Tests;

[Collection("WebFactory")]
public sealed class FollowupScenarioTests
{
    private readonly ProxyInvocationContext _context = new()
    {
        CorrelationId = "corr-s6",
        ClientRole = "operator",
        InboundHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["X-HTTP-Method-Override"] = "DELETE"
        }
    };

    [Fact]
    public async Task Scenario06_CallerCannotInfluenceOutboundMethod()
    {
        var fx = new ProxyHandlerFixture();
        // Operation is configured GET; body carries attacker-supplied method override fields.
        var op = ProxyHandlerFixture.BuildOperation(
            "method.locked",
            HttpVerb.Get,
            "/api/v1/locked",
            allowUnknownParameters: true);
        fx.Configuration.Setup(c => c.GetOperationAsync("method.locked", It.IsAny<CancellationToken>())).ReturnsAsync(op);
        fx.Configuration.Setup(c => c.GetDestinationAsync("dest", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProxyHandlerFixture.BuildDestination());
        var handler = fx.CreateHandler();

        using var bodyDoc = JsonDocument.Parse("""{"method":"DELETE","httpMethod":"PATCH"}""");
        await handler.HandleAsync(new ProxyExecuteRequest
        {
            ClientId = "method.locked",
            Body = bodyDoc.RootElement.Clone()
        }, _context);

        Assert.Equal(1, fx.Recorder.InvocationCount);
        Assert.Equal(HttpMethod.Get, fx.Recorder.Requests[0].Method);
    }

    [Fact]
    public async Task Scenario06_DisallowedEnvelopeMethod_RejectedAtRouting()
    {
        using var factory = new ProxyWebApplicationFactory("Production");
        using var client = factory.CreateClient();

        // The proxy envelope only accepts POST; a GET must be rejected by ASP.NET routing before the handler.
        var response = await client.GetAsync("/api/proxy/execute");

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task Scenario16_EfBacked_StaleRowVersion_ThrowsConcurrencyAndMapsTo409()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ProxyDbContext>()
            .UseSqlite(connection)
            .Options;

        var operationId = Guid.NewGuid();
        var destinationId = Guid.NewGuid();

        await using (var seed = new ProxyDbContext(options))
        {
            await seed.Database.EnsureCreatedAsync();

            // Seed via raw SQL so the rowversion (store-generated under SQL Server) gets a deterministic,
            // non-null value under SQLite, which does not auto-generate rowversion blobs.
            await seed.Database.ExecuteSqlRawAsync(
                "INSERT INTO Destinations (Id, Name, BaseUrl, IsActive, TimeoutMilliseconds, SettingsJson, RowVersion, CreatedAt, UpdatedAt) " +
                "VALUES ({0}, {1}, {2}, 1, 30000, NULL, {3}, {4}, {4})",
                destinationId, "dest-ef", "https://downstream.internal:8443/", new byte[] { 1 }, DateTime.UtcNow);

            await seed.Database.ExecuteSqlRawAsync(
                "INSERT INTO ProxyOperations (Id, OperationKey, DestinationId, HttpMethod, RelativePath, IsActive, MaxRequestSize, TimeoutMilliseconds, RequiresEncryption, AllowUnknownParameters, AllowHttpClientRetryForDelete, AllowedRoles, RowVersion) " +
                "VALUES ({0}, {1}, {2}, 'GET', {3}, 1, 1048576, NULL, 0, 0, 0, 'operator', {4})",
                operationId, "ef.op", destinationId, "/api/v1/ef", new byte[] { 1, 2, 3, 4 });
        }

        var factory = new SingleConnectionDbContextFactory(options);
        var admin = new ProxyOperationAdminService(
            factory,
            new StubCacheInvalidator(),
            new OptimisticConcurrencyUpdater(),
            NullLogger<ProxyOperationAdminService>.Instance);

        var view = await admin.GetOperationAsync(operationId);
        Assert.NotNull(view);
        var staleRowVersion = view!.RowVersion;

        // A competing writer bumps the RowVersion out from under us.
        await using (var competitor = new ProxyDbContext(options))
        {
            await competitor.Database.ExecuteSqlRawAsync(
                "UPDATE ProxyOperations SET RowVersion = {0} WHERE Id = {1}",
                new byte[] { 9, 9, 9, 9 }, operationId);
        }

        var conflict = await admin.UpdateOperationAsync(operationId, new UpdateProxyOperationRequest
        {
            RowVersion = staleRowVersion,
            RelativePath = "/api/v1/ef-updated"
        });
        Assert.False(conflict.Success);
        Assert.True(conflict.ConcurrencyConflict);

        // The GET after a failed update confirms the conflicting change did not apply.
        var after = await admin.GetOperationAsync(operationId);
        Assert.Equal("/api/v1/ef", after!.RelativePath);
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
