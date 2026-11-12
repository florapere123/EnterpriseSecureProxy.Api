using EnterpriseSecureProxy.Application.Dtos;
using EnterpriseSecureProxy.Application.UseCases;
using EnterpriseSecureProxy.Domain.Entities;
using EnterpriseSecureProxy.Domain.Enums;
using EnterpriseSecureProxy.Tests.Support;
using Moq;
using Xunit;

namespace EnterpriseSecureProxy.Tests;

public sealed class ResolveHeadersTests
{
    [Fact]
    public async Task AuthorizationForwardBypassingCreate_StillStrippedOnOutbound()
    {
        var fx = new ProxyHandlerFixture();
        var sneakyRule = HeaderRule.Rehydrate(
            Guid.NewGuid(),
            "Authorization",
            HeaderRuleMode.Forward,
            injectedValue: null,
            isRequired: false);

        var op = ProxyOperation.Rehydrate(
            Guid.NewGuid(), "sneaky", "sneaky", "dest", HttpVerb.Get, "/api/v1/operation", true, 1024, false,
            ClassificationLevel.Unclassified, Array.Empty<string>(),
            Array.Empty<ParameterRule>(),
            new[] { sneakyRule });

        fx.Configuration.Setup(c => c.GetOperationAsync("sneaky", It.IsAny<CancellationToken>())).ReturnsAsync(op);
        fx.Configuration.Setup(c => c.GetDestinationAsync("dest", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProxyHandlerFixture.BuildDestination());
        var handler = fx.CreateHandler();
        var context = new ProxyInvocationContext
        {
            CorrelationId = "c",
            InboundHeaders = new Dictionary<string, string>
            {
                ["Authorization"] = "Bearer must-not-leak"
            }
        };
        await handler.HandleAsync(new ProxyExecuteRequest { ClientId = "sneaky" }, context);
        Assert.False(fx.Recorder.Requests[0].Headers.Contains("Authorization"));
    }
}
