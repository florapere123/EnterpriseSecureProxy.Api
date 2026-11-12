using System.Net;
using EnterpriseSecureProxy.Application.Dtos;
using EnterpriseSecureProxy.Application.Interfaces;
using EnterpriseSecureProxy.Domain.Entities;
using EnterpriseSecureProxy.Domain.Enums;
using EnterpriseSecureProxy.Infrastructure.Configuration;
using EnterpriseSecureProxy.Infrastructure.Forwarding;
using Microsoft.Extensions.Options;
using EnterpriseSecureProxy.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EnterpriseSecureProxy.Tests;

public sealed class ForwarderRetryPolicyTests
{
    [Fact]
    public async Task Post_Returning503_IsInvokedOnce_NoRetry()
    {
        var recorder = new RecordingHttpMessageHandler
        {
            Responder = _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        };

        var forwarder = BuildForwarder(recorder);
        var operation = ProxyOperation.Rehydrate(
            Guid.NewGuid(), "p", "p", "d", HttpVerb.Post, "/x", true, 1024, false,
            ClassificationLevel.Unclassified, Array.Empty<string>(),
            Array.Empty<ParameterRule>(), Array.Empty<HeaderRule>());
        var destination = Destination.Create("d", new Uri("https://x/"));

        await forwarder.ForwardAsync(new ForwardRequest
        {
            CorrelationId = "c",
            Operation = operation,
            Destination = destination,
            TargetUri = new Uri("https://x/x"),
            Headers = new Dictionary<string, string>()
        });

        Assert.Equal(1, recorder.InvocationCount);
    }

    [Fact]
    public async Task Get_Returning503TwiceThenOk_IsInvokedThreeTimes()
    {
        var attempts = 0;
        var recorder = new RecordingHttpMessageHandler
        {
            Responder = _ =>
            {
                attempts++;
                return attempts < 3
                    ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                    : new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
                    };
            }
        };

        var forwarder = BuildForwarder(recorder);
        var operation = ProxyOperation.Rehydrate(
            Guid.NewGuid(), "g", "g", "d", HttpVerb.Get, "/x", true, 1024, false,
            ClassificationLevel.Unclassified, Array.Empty<string>(),
            Array.Empty<ParameterRule>(), Array.Empty<HeaderRule>());
        var destination = Destination.Create("d", new Uri("https://x/"));

        var result = await forwarder.ForwardAsync(new ForwardRequest
        {
            CorrelationId = "c",
            Operation = operation,
            Destination = destination,
            TargetUri = new Uri("https://x/x"),
            Headers = new Dictionary<string, string>()
        });

        Assert.Equal(3, recorder.InvocationCount);
        Assert.True(result.IsSuccess);
    }

    private static HttpProxyForwarder BuildForwarder(RecordingHttpMessageHandler handler)
    {
        var services = new ServiceCollection();
        services.AddHttpClient(HttpProxyForwarder.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => handler)
            .AddPolicyHandler(ForwarderResiliencePolicies.SelectRetryPolicy)
            .AddPolicyHandler(ForwarderResiliencePolicies.BuildCircuitBreakerPolicy());
        services.Configure<DownstreamOptions>(o => o.MaxConcurrentRequestsPerDestination = 32);
        services.AddSingleton(_ => new DestinationConcurrencyLimiter(Options.Create(new DownstreamOptions())));
        services.AddSingleton<HttpProxyForwarder>();
        services.AddSingleton<IProxyForwarder>(sp => sp.GetRequiredService<HttpProxyForwarder>());
        return services.BuildServiceProvider().GetRequiredService<HttpProxyForwarder>();
    }
}
