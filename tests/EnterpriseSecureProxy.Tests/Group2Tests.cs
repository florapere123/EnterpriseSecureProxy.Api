using EnterpriseSecureProxy.Domain.Audit;
using EnterpriseSecureProxy.Domain.Enums;
using EnterpriseSecureProxy.Infrastructure.DependencyInjection;
using EnterpriseSecureProxy.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnterpriseSecureProxy.Tests;

public sealed class Group2Tests
{
    [Theory]
    [InlineData("Mock")]
    [InlineData("SQL")]
    public void DependencyInjection_ResolvesCriticalServices(string dataSource)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DataSource"] = dataSource,
                ["ConnectionStrings:ProxyConfiguration"] = "Server=(localdb)\\mssqllocaldb;Database=ProxyTest;Trusted_Connection=True;TrustServerCertificate=True"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProxyInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<EnterpriseSecureProxy.Application.Interfaces.IProxyOperationAdminService>());
        Assert.NotNull(provider.GetService<EnterpriseSecureProxy.Application.Interfaces.IDestinationAdminService>());
    }

    [Fact]
    public void DestinationErrorAudit_IsMarkedForwarded()
    {
        Assert.True(AuditForwardingRules.WasForwardedToDestination(AuditOutcome.DestinationError));
        Assert.False(AuditForwardingRules.WasForwardedToDestination(AuditOutcome.ValidationFailed));
    }

    [Fact]
    public async Task MockAdmin_Update_WithStaleRowVersion_ThrowsConcurrency()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DataSource"] = "Mock" })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProxyInfrastructure(configuration);
        await using var provider = services.BuildServiceProvider();
        var admin = provider.GetRequiredService<EnterpriseSecureProxy.Application.Interfaces.IProxyOperationAdminService>();
        var operation = (await provider.GetRequiredService<EnterpriseSecureProxy.Domain.Interfaces.IConfigurationRepository>()
            .GetAllOperationsAsync()).First();
        var view = await admin.GetOperationAsync(operation.Id);
        Assert.NotNull(view);
        await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException>(() =>
            admin.UpdateOperationAsync(operation.Id, new EnterpriseSecureProxy.Application.Dtos.UpdateProxyOperationRequest
            {
                RowVersion = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 })
            }));
    }
}
