using EnterpriseSecureProxy.Application.Auditing;
using EnterpriseSecureProxy.Application.Steps;
using EnterpriseSecureProxy.Application.Dtos;
using EnterpriseSecureProxy.Application.UseCases;
using EnterpriseSecureProxy.Domain.Entities;
using EnterpriseSecureProxy.Domain.Enums;
using EnterpriseSecureProxy.Domain.Exceptions;
using EnterpriseSecureProxy.Domain.Interfaces;
using EnterpriseSecureProxy.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace EnterpriseSecureProxy.Tests;

public sealed class EncryptionStepTests
{
    [Fact]
    public async Task DecryptAsync_InvalidTag_FailClosedWithProxyDecryptionException_AndAudit()
    {
        var key = new SecuritySettings { MasterKeyBase64 = "QUJDREVGR0hJSktMTU5PUFFSU1RVVldYWVoxMjM0NTY=" };
        var encryption = new AesGcmEncryptionService(Options.Create(key));
        var sink = new Mock<IAuditSink>();
        sink.Setup(a => a.RecordAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var behavior = new EncryptionStep(encryption, new ProxyAuditRecorder(sink.Object));

        var operation = ProxyOperation.Rehydrate(
            Guid.NewGuid(),
            "alert.subscribe",
            "alert.subscribe",
            "telemetry.radar-feed",
            HttpVerb.Post,
            "/api/v1/alerts/subscribe",
            true,
            2048,
            requiresEncryption: true,
            ClassificationLevel.Unclassified,
            new[] { "AlertManager" },
            Array.Empty<ParameterRule>(),
            Array.Empty<HeaderRule>());

        var request = new ProxyExecuteRequest
        {
            ClientId = "alert.subscribe",
            Ciphertext = "oJgVpgBIqlCAR+VZ2PHdvHP8iEewEGkG6yYRDYI=",
            Nonce = "CQgHBgUEAwIBAAsK",
            Tag = "AAAAAAAAAAAAAAAAAAAAAA=="
        };

        var context = new ProxyInvocationContext { CorrelationId = "corr-crypto" };

        var ex = await Assert.ThrowsAsync<ProxyDecryptionException>(
            () => behavior.DecryptAsync(request, operation, context, elapsedMs: 1));

        Assert.Equal("alert.subscribe", ex.OperationKey);
        sink.Verify(
            a => a.RecordAsync(
                It.Is<AuditLog>(log => log.Outcome == AuditOutcome.RejectionCryptoFailure),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DecryptAsync_ValidEnvelope_ProducesDecryptedBody()
    {
        var key = new SecuritySettings { MasterKeyBase64 = "QUJDREVGR0hJSktMTU5PUFFSU1RVVldYWVoxMjM0NTY=" };
        var encryption = new AesGcmEncryptionService(Options.Create(key));

        var behavior = new EncryptionStep(
            encryption,
            new ProxyAuditRecorder(Mock.Of<IAuditSink>()));

        var operation = ProxyOperation.Rehydrate(
            Guid.NewGuid(),
            "alert.subscribe",
            "alert.subscribe",
            "telemetry.radar-feed",
            HttpVerb.Post,
            "/api/v1/alerts/subscribe",
            true,
            2048,
            requiresEncryption: true,
            ClassificationLevel.Unclassified,
            new[] { "AlertManager" },
            Array.Empty<ParameterRule>(),
            Array.Empty<HeaderRule>());

        var request = new ProxyExecuteRequest
        {
            ClientId = "alert.subscribe",
            Ciphertext = "oJgVpgBIqlCAR+VZ2PHdvHP8iEewEGkG6yYRDYI=",
            Nonce = "CQgHBgUEAwIBAAsK",
            Tag = "d3ICv6A3ICJhojqtYcGIHA=="
        };

        var effective = await behavior.DecryptAsync(
            request,
            operation,
            new ProxyInvocationContext { CorrelationId = "corr-ok" },
            elapsedMs: 1);

        Assert.NotNull(effective.Body);
        Assert.Equal("alerts-critical", effective.Body!.Value.GetProperty("channel").GetString());
    }
}
