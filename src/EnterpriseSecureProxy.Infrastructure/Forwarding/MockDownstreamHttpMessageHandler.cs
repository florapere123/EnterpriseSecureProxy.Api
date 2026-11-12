using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;

namespace EnterpriseSecureProxy.Infrastructure.Forwarding;

/// <summary>
/// Simulates internal microservices for E2E verification. Logs metadata only (no decrypted body content).
/// </summary>
public sealed class MockDownstreamHttpMessageHandler : HttpMessageHandler
{
    private readonly ILogger<MockDownstreamHttpMessageHandler> _logger;

    public MockDownstreamHttpMessageHandler(ILogger<MockDownstreamHttpMessageHandler> logger) =>
        _logger = logger;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var payloadBytes = 0;
        if (request.Content is not null)
        {
            var raw = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            payloadBytes = raw.Length;
        }

        Console.WriteLine(
            $"[MockDestination] {request.Method} {request.RequestUri} | PayloadBytes={payloadBytes} (content not logged)");
        _logger.LogInformation(
            "Mock downstream received {Method} {Uri} with payload length {PayloadBytes} bytes.",
            request.Method,
            request.RequestUri,
            payloadBytes);

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"status":"received","mock":true}""",
                Encoding.UTF8,
                "application/json")
        };
    }
}
