using EnterpriseSecureProxy.Domain.Entities;
using EnterpriseSecureProxy.Domain.Enums;
using EnterpriseSecureProxy.Domain.Interfaces;
using EnterpriseSecureProxy.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseSecureProxy.Infrastructure.Repositories;

/// <summary>Hardcoded routes and destinations for local testing without SQL.</summary>
public sealed class MockConfigurationRepository : IConfigurationRepository
{
    private readonly IReadOnlyDictionary<string, ProxyOperation> _operations;
    private readonly IReadOnlyDictionary<string, Destination> _destinations;

    public MockConfigurationRepository(
        ILogger<MockConfigurationRepository> logger,
        IOptions<ProxyDefaultsOptions> defaults)
    {
        var options = defaults.Value;
        var destinations = BuildDestinations(options);
        var operations = BuildOperations(options);

        _destinations = destinations.ToDictionary(d => d.Name, StringComparer.OrdinalIgnoreCase);
        _operations = operations.ToDictionary(o => o.OperationKey, StringComparer.Ordinal);

        logger.LogInformation(
            "MockConfigurationRepository initialized with {OperationCount} operations and {DestinationCount} destinations.",
            _operations.Count, _destinations.Count);
    }

    public Task<ProxyOperation?> GetOperationAsync(string operationKey, CancellationToken cancellationToken = default)
    {
        _operations.TryGetValue(operationKey, out var operation);
        return Task.FromResult(operation is { IsEnabled: true } ? operation : null);
    }

    public Task<Destination?> GetDestinationAsync(string destinationName, CancellationToken cancellationToken = default)
    {
        _destinations.TryGetValue(destinationName, out var destination);
        return Task.FromResult(destination is { IsEnabled: true } ? destination : null);
    }

    public Task<IReadOnlyList<ProxyOperation>> GetAllOperationsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ProxyOperation>>(_operations.Values.ToList());

    private static Destination[] BuildDestinations(ProxyDefaultsOptions options)
    {
        var standardTimeout = TimeSpan.FromSeconds(options.DefaultDestinationTimeoutSeconds);
        var slowTimeout = TimeSpan.FromSeconds(options.SlowDestinationTimeoutSeconds);

        return
        [
            Destination.Rehydrate(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "target-api",
                new Uri("https://targets.local"),
                standardTimeout,
                isEnabled: true,
                description: "Target location API"),
            Destination.Rehydrate(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "threat-api",
                new Uri("https://threats.local"),
                standardTimeout,
                isEnabled: true,
                description: "Threat tracking API"),
            Destination.Rehydrate(
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                "vision-api",
                new Uri("https://vision.local"),
                slowTimeout,
                isEnabled: true,
                description: "Vision / ISR data API"),
        ];
    }

    private static ProxyOperation[] BuildOperations(ProxyDefaultsOptions options)
    {
        var maxRequestSize = options.DefaultMaxRequestSizeBytes;

        return
        [
            ProxyOperation.Rehydrate(
                id: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                operationKey: "target.location",
                displayName: "Target location",
                destinationName: "target-api",
                method: HttpVerb.Post,
                relativePath: "/targets",
                isEnabled: true,
                maxRequestSize: maxRequestSize,
                requiresEncryption: false,
                requiredClassification: ClassificationLevel.Confidential,
                allowedRoles: new[] { "operator", "user" },
                parameterRules: new[]
                {
                    ParameterRule.Create("format", ParameterLocation.Body, ParameterDataType.String,
                        allowedValues: new[] { "MGRS", "LatLong" }),
                    ParameterRule.Create("value", ParameterLocation.Body, ParameterDataType.String)
                },
                headerRules: new[]
                {
                    HeaderRule.Create("X-Correlation-Id", HeaderRuleMode.Forward),
                    HeaderRule.Create("X-Proxy-Origin", HeaderRuleMode.Inject, injectedValue: "EnterpriseSecureProxy")
                },
                description: "Report MGRS or LatLong coordinates."),
            ProxyOperation.Rehydrate(
                id: Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                operationKey: "threat.update",
                displayName: "Threat level",
                destinationName: "threat-api",
                method: HttpVerb.Put,
                relativePath: "/threats",
                isEnabled: true,
                maxRequestSize: maxRequestSize,
                requiresEncryption: false,
                requiredClassification: ClassificationLevel.Secret,
                allowedRoles: new[] { "admin", "operator" },
                parameterRules: new[]
                {
                    ParameterRule.Create("id", ParameterLocation.Body, ParameterDataType.Guid),
                    ParameterRule.Create("level", ParameterLocation.Body, ParameterDataType.String,
                        allowedValues: new[] { "Unclassified", "Restricted", "Confidential", "Secret", "TopSecret" })
                },
                headerRules: new[]
                {
                    HeaderRule.Create("X-Correlation-Id", HeaderRuleMode.Forward),
                    HeaderRule.Create("Authorization", HeaderRuleMode.Inject, injectedValue: "secretref:threat-bearer")
                },
                description: "Update threat classification."),
            ProxyOperation.Rehydrate(
                id: Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                operationKey: "vision.get",
                displayName: "Vision fetch",
                destinationName: "vision-api",
                method: HttpVerb.Post,
                relativePath: "/vision",
                isEnabled: true,
                maxRequestSize: maxRequestSize,
                requiresEncryption: false,
                requiredClassification: ClassificationLevel.Confidential,
                allowedRoles: new[] { "operator", "user" },
                parameterRules: new[]
                {
                    ParameterRule.Create("urlId", ParameterLocation.Body, ParameterDataType.String,
                        pattern: "^[A-Za-z0-9_-]{4,128}$")
                },
                headerRules: new[]
                {
                    HeaderRule.Create("X-Correlation-Id", HeaderRuleMode.Forward),
                    HeaderRule.Create("X-Proxy-Origin", HeaderRuleMode.Inject, injectedValue: "EnterpriseSecureProxy")
                },
                description: "Fetch vision data by urlId."),
        ];
    }
}
