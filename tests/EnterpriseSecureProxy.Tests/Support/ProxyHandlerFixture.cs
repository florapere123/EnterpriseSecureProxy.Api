using EnterpriseSecureProxy.Application.Auditing;
using EnterpriseSecureProxy.Application.Steps;
using EnterpriseSecureProxy.Application.Interfaces;
using EnterpriseSecureProxy.Application.UseCases;
using EnterpriseSecureProxy.Application.Validation;
using EnterpriseSecureProxy.Domain.Entities;
using EnterpriseSecureProxy.Domain.Enums;
using EnterpriseSecureProxy.Domain.Interfaces;
using EnterpriseSecureProxy.Infrastructure.Caching;
using EnterpriseSecureProxy.Infrastructure.Configuration;
using EnterpriseSecureProxy.Infrastructure.Forwarding;
using EnterpriseSecureProxy.Infrastructure.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace EnterpriseSecureProxy.Tests.Support;

public sealed class ProxyHandlerFixture
{
    public RecordingHttpMessageHandler Recorder { get; } = new();
    public CaptureAuditSink Audit { get; } = new();
    public Mock<IConfigurationRepository> Configuration { get; } = new();
    public IProxyForwarder Forwarder { get; }

    public ProxyHandlerFixture()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient(HttpProxyForwarder.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => Recorder)
            .AddPolicyHandler(ForwarderResiliencePolicies.SelectRetryPolicy)
            .AddPolicyHandler(ForwarderResiliencePolicies.BuildCircuitBreakerPolicy());
        services.Configure<DownstreamOptions>(o => o.MaxConcurrentRequestsPerDestination = 32);
        services.AddSingleton<DestinationConcurrencyLimiter>();
        services.AddSingleton<IProxyForwarder, HttpProxyForwarder>();
        Forwarder = services.BuildServiceProvider().GetRequiredService<IProxyForwarder>();
    }

    public ExecuteProxyOperationHandler CreateHandler()
    {
        var key = new SecuritySettings { MasterKeyBase64 = "QUJDREVGR0hJSktMTU5PUFFSU1RVVldYWVoxMjM0NTY=" };
        var auditRecorder = new ProxyAuditRecorder(Audit);
        var encryption = new EncryptionStep(
            new AesGcmEncryptionService(Options.Create(key)),
            auditRecorder);
        var idempotency = new IdempotencyStep(
            new MemoryIdempotencyResponseCache(new MemoryCache(new MemoryCacheOptions())),
            auditRecorder);

        return new ExecuteProxyOperationHandler(
            Configuration.Object,
            new ParameterRuleValidator(),
            Forwarder,
            auditRecorder,
            encryption,
            idempotency);
    }

    public static ProxyOperation BuildOperation(
        string key,
        HttpVerb method = HttpVerb.Get,
        string path = "/api/v1/operation",
        string destination = "dest",
        bool enabled = true,
        bool requiresEncryption = false,
        bool allowUnknownParameters = false,
        IReadOnlyList<ParameterRule>? rules = null,
        IReadOnlyList<HeaderRule>? headers = null,
        IReadOnlyList<string>? roles = null) =>
        ProxyOperation.Rehydrate(
            Guid.NewGuid(),
            key,
            key,
            destination,
            method,
            path,
            enabled,
            1_048_576,
            requiresEncryption,
            ClassificationLevel.Unclassified,
            roles ?? Array.Empty<string>(),
            rules ?? Array.Empty<ParameterRule>(),
            headers ?? Array.Empty<HeaderRule>(),
            allowUnknownParameters: allowUnknownParameters);

    public static Destination BuildDestination(string name = "dest", bool enabled = true) =>
        Destination.Create(name, new Uri("https://downstream.internal:8443/"), TimeSpan.FromSeconds(30), enabled);
}
