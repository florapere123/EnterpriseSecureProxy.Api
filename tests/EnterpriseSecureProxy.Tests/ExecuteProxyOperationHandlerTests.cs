using EnterpriseSecureProxy.Application.Auditing;
using EnterpriseSecureProxy.Application.Steps;
using EnterpriseSecureProxy.Application.Dtos;
using EnterpriseSecureProxy.Application.Interfaces;
using EnterpriseSecureProxy.Application.UseCases;
using EnterpriseSecureProxy.Application.Validation;
using EnterpriseSecureProxy.Domain.Entities;
using EnterpriseSecureProxy.Domain.Enums;
using EnterpriseSecureProxy.Domain.Exceptions;
using EnterpriseSecureProxy.Domain.Interfaces;
using EnterpriseSecureProxy.Infrastructure.Caching;
using EnterpriseSecureProxy.Infrastructure.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace EnterpriseSecureProxy.Tests;

public sealed class ExecuteProxyOperationHandlerTests
{
    [Fact]
    public async Task HandleAsync_InvalidRole_ThrowsOperationForbidden_ForHttp403Path()
    {
        var operation = ProxyOperation.Rehydrate(
            Guid.NewGuid(),
            "sensor.reading.get",
            "sensor.reading.get",
            "telemetry.radar-feed",
            HttpVerb.Get,
            "/api/v1/sensors/reading",
            true,
            8192,
            requiresEncryption: false,
            ClassificationLevel.Unclassified,
            new[] { "SensorClient" },
            new[]
            {
                ParameterRule.Create("sensorId", ParameterLocation.Query, ParameterDataType.String, isRequired: true)
            },
            Array.Empty<HeaderRule>());

        var config = new Mock<IConfigurationRepository>();
        config.Setup(c => c.GetOperationAsync("sensor.reading.get", It.IsAny<CancellationToken>()))
            .ReturnsAsync(operation);

        var forwarder = new Mock<IProxyForwarder>(MockBehavior.Strict);

        var key = new SecuritySettings { MasterKeyBase64 = "QUJDREVGR0hJSktMTU5PUFFSU1RVVldYWVoxMjM0NTY=" };
        var auditRecorder = new ProxyAuditRecorder(Mock.Of<IAuditSink>());
        var encryption = new EncryptionStep(
            new AesGcmEncryptionService(Options.Create(key)),
            auditRecorder);
        var idempotency = new IdempotencyStep(
            new MemoryIdempotencyResponseCache(new MemoryCache(new MemoryCacheOptions())),
            auditRecorder);

        var handler = new ExecuteProxyOperationHandler(
            config.Object,
            new ParameterRuleValidator(),
            forwarder.Object,
            auditRecorder,
            encryption,
            idempotency);

        var request = new ProxyExecuteRequest
        {
            ClientId = "sensor.reading.get",
            Parameters = new Dictionary<string, string?> { ["sensorId"] = "RADAR-42" }
        };

        var context = new ProxyInvocationContext
        {
            CorrelationId = "corr-rbac",
            ClientRole = "WrongRole"
        };

        var ex = await Assert.ThrowsAsync<OperationForbiddenException>(
            () => handler.HandleAsync(request, context));

        Assert.Equal("sensor.reading.get", ex.OperationKey);
        forwarder.Verify(
            f => f.ForwardAsync(It.IsAny<ForwardRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
