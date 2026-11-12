using System.Net;
using System.Text.Json;
using EnterpriseSecureProxy.Application.Dtos;
using EnterpriseSecureProxy.Application.UseCases;
using EnterpriseSecureProxy.Domain.Entities;
using EnterpriseSecureProxy.Domain.Enums;
using EnterpriseSecureProxy.Domain.Exceptions;
using EnterpriseSecureProxy.Tests.Support;
using Moq;
using Xunit;

namespace EnterpriseSecureProxy.Tests;

/// <summary>Assessment spec §9 scenarios (handler + recording downstream).</summary>
public sealed class SpecScenarioTests
{
    private readonly ProxyInvocationContext _context = new()
    {
        CorrelationId = "corr-1",
        ClientRole = "operator",
        InboundHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Authorization"] = "Bearer attacker-token",
            ["X-Correlation-Id"] = "corr-1"
        }
    };

    [Fact]
    public async Task Scenario01_ValidOperation_ForwardedSuccessfully()
    {
        var fx = new ProxyHandlerFixture();
        var op = ProxyHandlerFixture.BuildOperation("sensor.get", HttpVerb.Get, "/api/v1/sensors/{sensorId}", rules: new[]
        {
            ParameterRule.Create("sensorId", ParameterLocation.Route, ParameterDataType.String)
        });
        fx.Configuration.Setup(c => c.GetOperationAsync("sensor.get", It.IsAny<CancellationToken>())).ReturnsAsync(op);
        fx.Configuration.Setup(c => c.GetDestinationAsync("dest", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProxyHandlerFixture.BuildDestination());
        var handler = fx.CreateHandler();
        var response = await handler.HandleAsync(new ProxyExecuteRequest
        {
            ClientId = "sensor.get",
            Parameters = new Dictionary<string, string?> { ["sensorId"] = "RADAR-1" }
        }, _context);

        Assert.True(response.Success);
        Assert.Equal(1, fx.Recorder.InvocationCount);
    }

    [Fact]
    public async Task Scenario02_UnknownOperation_Rejected_HandlerNeverCalled()
    {
        var fx = new ProxyHandlerFixture();
        fx.Configuration.Setup(c => c.GetOperationAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProxyOperation?)null);
        var handler = fx.CreateHandler();
        var response = await handler.HandleAsync(new ProxyExecuteRequest { ClientId = "missing" }, _context);
        Assert.False(response.Success);
        Assert.Equal("NOT_FOUND", response.Status);
        Assert.Equal(0, fx.Recorder.InvocationCount);
    }

    [Fact]
    public async Task Scenario03_DisabledOperation_Rejected_HandlerNeverCalled()
    {
        var fx = new ProxyHandlerFixture();
        fx.Configuration.Setup(c => c.GetOperationAsync("off", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProxyOperation?)null);
        var handler = fx.CreateHandler();
        var response = await handler.HandleAsync(new ProxyExecuteRequest { ClientId = "off" }, _context);
        Assert.Equal("NOT_FOUND", response.Status);
        Assert.Equal(0, fx.Recorder.InvocationCount);
    }

    [Fact]
    public async Task Scenario04_DisabledDestination_Rejected_HandlerNeverCalled()
    {
        var fx = new ProxyHandlerFixture();
        var op = ProxyHandlerFixture.BuildOperation("x");
        fx.Configuration.Setup(c => c.GetOperationAsync("x", It.IsAny<CancellationToken>())).ReturnsAsync(op);
        fx.Configuration.Setup(c => c.GetDestinationAsync("dest", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Destination?)null);
        var handler = fx.CreateHandler();
        var response = await handler.HandleAsync(new ProxyExecuteRequest { ClientId = "x" }, _context);
        Assert.Equal("DESTINATION_UNAVAILABLE", response.Status);
        Assert.Equal(0, fx.Recorder.InvocationCount);
    }

    [Fact]
    public async Task Scenario05_CallerCannotOverrideTrustedDestination_HandlerNeverCalledWithAttackerTarget()
    {
        var fx = new ProxyHandlerFixture();
        var op = ProxyHandlerFixture.BuildOperation("safe", HttpVerb.Get, "/api/v1/safe", rules: new[]
        {
            ParameterRule.Create("q", ParameterLocation.Query, ParameterDataType.String, isRequired: false)
        });
        fx.Configuration.Setup(c => c.GetOperationAsync("safe", It.IsAny<CancellationToken>())).ReturnsAsync(op);
        fx.Configuration.Setup(c => c.GetDestinationAsync("dest", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProxyHandlerFixture.BuildDestination());
        var handler = fx.CreateHandler();
        await handler.HandleAsync(new ProxyExecuteRequest
        {
            ClientId = "safe",
            Parameters = new Dictionary<string, string?>
            {
                ["q"] = "1",
                ["destinationOverride"] = "https://evil.com",
                ["relativePath"] = "/admin",
                ["method"] = "DELETE"
            }
        }, _context);

        Assert.Equal(0, fx.Recorder.InvocationCount);
        Assert.Contains(fx.Audit.Entries, e => e.Outcome == AuditOutcome.ValidationFailed);
    }

    [Fact]
    public async Task Scenario07_MissingRequiredParameter_Rejected_HandlerNeverCalled()
    {
        var fx = new ProxyHandlerFixture();
        var op = ProxyHandlerFixture.BuildOperation("r", rules: new[]
        {
            ParameterRule.Create("id", ParameterLocation.Query, ParameterDataType.String, isRequired: true)
        });
        fx.Configuration.Setup(c => c.GetOperationAsync("r", It.IsAny<CancellationToken>())).ReturnsAsync(op);
        fx.Configuration.Setup(c => c.GetDestinationAsync("dest", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProxyHandlerFixture.BuildDestination());
        var handler = fx.CreateHandler();
        var response = await handler.HandleAsync(new ProxyExecuteRequest { ClientId = "r" }, _context);
        Assert.Equal("VALIDATION_FAILED", response.Status);
        Assert.Equal(0, fx.Recorder.InvocationCount);
    }

    [Fact]
    public async Task Scenario08_UnknownParameter_Rejected_HandlerNeverCalled()
    {
        var fx = new ProxyHandlerFixture();
        var op = ProxyHandlerFixture.BuildOperation("u", rules: new[]
        {
            ParameterRule.Create("a", ParameterLocation.Query, ParameterDataType.String)
        });
        fx.Configuration.Setup(c => c.GetOperationAsync("u", It.IsAny<CancellationToken>())).ReturnsAsync(op);
        fx.Configuration.Setup(c => c.GetDestinationAsync("dest", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProxyHandlerFixture.BuildDestination());
        var handler = fx.CreateHandler();
        var response = await handler.HandleAsync(new ProxyExecuteRequest
        {
            ClientId = "u",
            Parameters = new Dictionary<string, string?> { ["a"] = "1", ["evil"] = "1" }
        }, _context);
        Assert.Equal("VALIDATION_FAILED", response.Status);
        Assert.Contains(response.Errors, e => e.Category == "UNKNOWN_PARAMETER");
        Assert.Equal(0, fx.Recorder.InvocationCount);
    }

    [Fact]
    public async Task Scenario09_PathTraversal_Rejected_HandlerNeverCalled()
    {
        var fx = new ProxyHandlerFixture();
        var op = ProxyHandlerFixture.BuildOperation("pt", HttpVerb.Get, "/api/v1/items/{id}", rules: new[]
        {
            ParameterRule.Create("id", ParameterLocation.Route, ParameterDataType.String)
        });
        fx.Configuration.Setup(c => c.GetOperationAsync("pt", It.IsAny<CancellationToken>())).ReturnsAsync(op);
        fx.Configuration.Setup(c => c.GetDestinationAsync("dest", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProxyHandlerFixture.BuildDestination());
        var handler = fx.CreateHandler();
        var response = await handler.HandleAsync(new ProxyExecuteRequest
        {
            ClientId = "pt",
            Parameters = new Dictionary<string, string?> { ["id"] = "../etc/passwd" }
        }, _context);
        Assert.Equal("VALIDATION_FAILED", response.Status);
        Assert.Equal(0, fx.Recorder.InvocationCount);
    }

    [Fact]
    public async Task Scenario09b_EncodedPathTraversal_Rejected_HandlerNeverCalled()
    {
        var fx = new ProxyHandlerFixture();
        var op = ProxyHandlerFixture.BuildOperation("pt2", HttpVerb.Get, "/api/v1/items/{id}", rules: new[]
        {
            ParameterRule.Create("id", ParameterLocation.Route, ParameterDataType.String)
        });
        fx.Configuration.Setup(c => c.GetOperationAsync("pt2", It.IsAny<CancellationToken>())).ReturnsAsync(op);
        fx.Configuration.Setup(c => c.GetDestinationAsync("dest", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProxyHandlerFixture.BuildDestination());
        var handler = fx.CreateHandler();
        var response = await handler.HandleAsync(new ProxyExecuteRequest
        {
            ClientId = "pt2",
            Parameters = new Dictionary<string, string?> { ["id"] = "%2e%2e%2f" }
        }, _context);
        Assert.Equal("VALIDATION_FAILED", response.Status);
        Assert.Equal(0, fx.Recorder.InvocationCount);
    }

    [Fact]
    public async Task Scenario10_SensitiveAuthorizationHeader_NeverForwarded()
    {
        var fx = new ProxyHandlerFixture();
        var op = ProxyHandlerFixture.BuildOperation("h", headers: new[]
        {
            HeaderRule.Create("X-Correlation-Id", HeaderRuleMode.Forward)
        });
        fx.Configuration.Setup(c => c.GetOperationAsync("h", It.IsAny<CancellationToken>())).ReturnsAsync(op);
        fx.Configuration.Setup(c => c.GetDestinationAsync("dest", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProxyHandlerFixture.BuildDestination());
        var handler = fx.CreateHandler();
        await handler.HandleAsync(new ProxyExecuteRequest { ClientId = "h" }, _context);
        Assert.Equal(1, fx.Recorder.InvocationCount);
        var outbound = fx.Recorder.Requests[0];
        Assert.False(outbound.Headers.Contains("Authorization"));
    }

    [Fact]
    public async Task Scenario11_InvalidAesTag_Rejected_HandlerNeverCalled()
    {
        var fx = new ProxyHandlerFixture();
        var op = ProxyHandlerFixture.BuildOperation("enc", HttpVerb.Post, "/x", requiresEncryption: true, rules: Array.Empty<ParameterRule>());
        fx.Configuration.Setup(c => c.GetOperationAsync("enc", It.IsAny<CancellationToken>())).ReturnsAsync(op);
        var handler = fx.CreateHandler();
        await Assert.ThrowsAsync<ProxyDecryptionException>(() => handler.HandleAsync(new ProxyExecuteRequest
        {
            ClientId = "enc",
            Ciphertext = "AA==",
            Nonce = "CQgHBgUEAwIBAAsK",
            Tag = "AAAAAAAAAAAAAAAAAAAAAA=="
        }, new ProxyInvocationContext { CorrelationId = "c" }));
        Assert.Equal(0, fx.Recorder.InvocationCount);
    }

    [Fact]
    public async Task Scenario12_InvalidEncryptedPayload_Rejected_HandlerNeverCalled()
    {
        var fx = new ProxyHandlerFixture();
        var op = ProxyHandlerFixture.BuildOperation("enc2", HttpVerb.Post, "/x", requiresEncryption: true);
        fx.Configuration.Setup(c => c.GetOperationAsync("enc2", It.IsAny<CancellationToken>())).ReturnsAsync(op);
        var handler = fx.CreateHandler();
        await Assert.ThrowsAsync<ProxyDecryptionException>(() => handler.HandleAsync(
            new ProxyExecuteRequest { ClientId = "enc2" },
            new ProxyInvocationContext { CorrelationId = "c" }));
        Assert.Equal(0, fx.Recorder.InvocationCount);
    }

    [Fact]
    public async Task Scenario13_UpstreamTimeout_ReturnsGatewayTimeout()
    {
        var fx = new ProxyHandlerFixture();
        fx.Recorder.ResponderAsync = async (_, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        };
        var op = ProxyHandlerFixture.BuildOperation("slow");
        fx.Configuration.Setup(c => c.GetOperationAsync("slow", It.IsAny<CancellationToken>())).ReturnsAsync(op);
        fx.Configuration.Setup(c => c.GetDestinationAsync("dest", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Destination.Create("dest", new Uri("https://x/"), TimeSpan.FromMilliseconds(50)));
        var handler = fx.CreateHandler();
        var response = await handler.HandleAsync(new ProxyExecuteRequest { ClientId = "slow" }, _context);
        Assert.Equal("DESTINATION_ERROR", response.Status);
        Assert.Equal(504, response.DownstreamStatusCode);
    }

    [Fact]
    public async Task Scenario14_Upstream500_MappedAsDestinationError()
    {
        var fx = new ProxyHandlerFixture();
        fx.Recorder.Responder = _ => new HttpResponseMessage(HttpStatusCode.InternalServerError);
        var op = ProxyHandlerFixture.BuildOperation("err");
        fx.Configuration.Setup(c => c.GetOperationAsync("err", It.IsAny<CancellationToken>())).ReturnsAsync(op);
        fx.Configuration.Setup(c => c.GetDestinationAsync("dest", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProxyHandlerFixture.BuildDestination());
        var handler = fx.CreateHandler();
        var response = await handler.HandleAsync(new ProxyExecuteRequest { ClientId = "err" }, _context);
        Assert.False(response.Success);
        Assert.Equal("DESTINATION_ERROR", response.Status);
        Assert.Equal(500, response.DownstreamStatusCode);
    }

    [Fact]
    public async Task Scenario15_CallerCancellation_Propagates_NotReportedAsTimeout()
    {
        var fx = new ProxyHandlerFixture();
        fx.Recorder.ResponderAsync = async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        };
        var op = ProxyHandlerFixture.BuildOperation("cancel");
        fx.Configuration.Setup(c => c.GetOperationAsync("cancel", It.IsAny<CancellationToken>())).ReturnsAsync(op);
        fx.Configuration.Setup(c => c.GetDestinationAsync("dest", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProxyHandlerFixture.BuildDestination());
        var handler = fx.CreateHandler();
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(50);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handler.HandleAsync(
            new ProxyExecuteRequest { ClientId = "cancel" }, _context, cts.Token));
    }

    [Fact]
    public async Task Scenario17_AuditRecords_NoSecretsInDetail()
    {
        var fx = new ProxyHandlerFixture();
        var failOp = ProxyHandlerFixture.BuildOperation("bad", rules: new[]
        {
            ParameterRule.Create("a", ParameterLocation.Query, ParameterDataType.String)
        });
        fx.Configuration.Setup(c => c.GetOperationAsync("bad", It.IsAny<CancellationToken>())).ReturnsAsync(failOp);
        fx.Configuration.Setup(c => c.GetDestinationAsync("dest", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProxyHandlerFixture.BuildDestination());
        var handler = fx.CreateHandler();
        await handler.HandleAsync(new ProxyExecuteRequest { ClientId = "bad" }, _context);

        var okOp = ProxyHandlerFixture.BuildOperation("good");
        fx.Configuration.Setup(c => c.GetOperationAsync("good", It.IsAny<CancellationToken>())).ReturnsAsync(okOp);
        await handler.HandleAsync(new ProxyExecuteRequest { ClientId = "good" }, _context);

        Assert.Contains(fx.Audit.Entries, e => e.Outcome == AuditOutcome.ValidationFailed);
        Assert.Contains(fx.Audit.Entries, e => e.Outcome == AuditOutcome.Succeeded);
        foreach (var entry in fx.Audit.Entries)
        {
            Assert.DoesNotContain("Bearer", entry.Detail ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Authorization", entry.Detail ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }
    }
}
