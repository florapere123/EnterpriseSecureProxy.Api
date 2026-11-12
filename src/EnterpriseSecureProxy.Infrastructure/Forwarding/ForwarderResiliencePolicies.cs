using Polly;
using Polly.Extensions.Http;

namespace EnterpriseSecureProxy.Infrastructure.Forwarding;

/// <summary>Polly retry and circuit-breaker policies for outbound calls.</summary>
internal static class ForwarderResiliencePolicies
{
    // OWASP Insecure Design: only retry when the forwarder marks the verb as safe to retry (typically GET).
    internal static IAsyncPolicy<HttpResponseMessage> SelectRetryPolicy(HttpRequestMessage request)
    {
        var allowRetry = request.Options.TryGetValue(ForwarderHttpRequestOptions.AllowHttpRetry, out var value) && value;
        return allowRetry ? BuildRetryPolicy() : Policy.NoOpAsync<HttpResponseMessage>();
    }

    /// OWASP Insecure Design: POST/PUT/PATCH never use this retry policy to avoid duplicating side effects on transient errors.
    internal static IAsyncPolicy<HttpResponseMessage> BuildRetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt - 1)));

    internal static IAsyncPolicy<HttpResponseMessage> BuildCircuitBreakerPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
}
