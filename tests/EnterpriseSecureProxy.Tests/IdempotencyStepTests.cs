using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EnterpriseSecureProxy.Application.Auditing;
using EnterpriseSecureProxy.Application.Dtos;
using EnterpriseSecureProxy.Application.Steps;
using EnterpriseSecureProxy.Application.Interfaces;
using EnterpriseSecureProxy.Application.UseCases;
using EnterpriseSecureProxy.Domain.Entities;
using EnterpriseSecureProxy.Domain.Enums;
using EnterpriseSecureProxy.Domain.Interfaces;
using EnterpriseSecureProxy.Infrastructure.Caching;
using EnterpriseSecureProxy.Tests.Support;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace EnterpriseSecureProxy.Tests;

public sealed class IdempotencyStepTests
{
    [Fact]
    public void TryGetCachedResponse_ReturnsCachedBody_AndWritesIdempotencyAudit_WithoutDownstream()
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new MemoryIdempotencyResponseCache(memoryCache);
        var requestBody = JsonSerializer.SerializeToElement(new { channel = "alerts-critical" });
        var cachedBody = JsonSerializer.SerializeToElement(new { subscriptionId = "sub-1" });
        cache.Store("alert.subscribe", "idem-1", new IdempotentDownstreamResponse
        {
            StatusCode = 200,
            Body = cachedBody,
            RequestBodyHash = HashBody(requestBody)
        });

        var auditSink = new Mock<IAuditSink>();
        auditSink.Setup(a => a.RecordAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var step = new IdempotencyStep(cache, new ProxyAuditRecorder(auditSink.Object));
        var operation = BuildPostOperation();
        var context = new ProxyInvocationContext
        {
            CorrelationId = "corr-1",
            IdempotencyKey = "idem-1",
            PrincipalId = "tester"
        };
        var request = new ProxyExecuteRequest { ClientId = "alert.subscribe", Body = requestBody };

        var replay = step.TryGetCachedResponse(operation, context, "alert.subscribe", request, elapsedMs: 5);

        Assert.NotNull(replay);
        Assert.Equal("OK", replay!.Status);
        Assert.Equal(200, replay.DownstreamStatusCode);

        auditSink.Verify(
            a => a.RecordAsync(
                It.Is<AuditLog>(log => log.Outcome == AuditOutcome.ServedFromIdempotencyCache),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SameKey_SameBody_ReplaysCachedResponse_DestinationCalledOnce()
    {
        var fx = BuildPostFixture();
        var handler = fx.CreateHandler();
        var body = Parse("""{"channel":"alerts-critical"}""");
        var context = PostContext("idem-same");

        var first = await handler.HandleAsync(Request(body), context);
        var second = await handler.HandleAsync(Request(body), context);

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Equal(1, fx.Recorder.InvocationCount);
    }

    [Fact]
    public async Task SameKey_DifferentBody_Rejected_DestinationNeverCalledForSecond()
    {
        var fx = BuildPostFixture();
        var handler = fx.CreateHandler();
        var context = PostContext("idem-diff");

        var first = await handler.HandleAsync(Request(Parse("""{"channel":"a"}""")), context);
        var second = await handler.HandleAsync(Request(Parse("""{"channel":"b"}""")), context);

        Assert.True(first.Success);
        Assert.False(second.Success);
        Assert.Equal("IDEMPOTENCY_KEY_REUSED_WITH_DIFFERENT_BODY", second.Status);
        // Only the first request reached the destination; the mismatched replay never forwards.
        Assert.Equal(1, fx.Recorder.InvocationCount);
        Assert.Contains(fx.Audit.Entries, e => e.Outcome == AuditOutcome.IdempotencyKeyReused);
    }

    private static ProxyHandlerFixture BuildPostFixture()
    {
        var fx = new ProxyHandlerFixture();
        var op = ProxyHandlerFixture.BuildOperation(
            "alert.subscribe", HttpVerb.Post, "/api/v1/alerts/subscribe", allowUnknownParameters: true);
        fx.Configuration.Setup(c => c.GetOperationAsync("alert.subscribe", It.IsAny<CancellationToken>())).ReturnsAsync(op);
        fx.Configuration.Setup(c => c.GetDestinationAsync("dest", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProxyHandlerFixture.BuildDestination());
        return fx;
    }

    private static ProxyInvocationContext PostContext(string idempotencyKey) => new()
    {
        CorrelationId = "corr-idem",
        IdempotencyKey = idempotencyKey
    };

    private static ProxyExecuteRequest Request(JsonElement body) =>
        new() { ClientId = "alert.subscribe", Body = body };

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement.Clone();

    private static ProxyOperation BuildPostOperation() => ProxyOperation.Rehydrate(
        Guid.NewGuid(),
        "alert.subscribe",
        "alert.subscribe",
        "dest",
        HttpVerb.Post,
        "/api/v1/alerts/subscribe",
        true,
        2048,
        false,
        ClassificationLevel.Unclassified,
        Array.Empty<string>(),
        Array.Empty<ParameterRule>(),
        Array.Empty<HeaderRule>());

    private static string HashBody(JsonElement body) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body.GetRawText())));
}
