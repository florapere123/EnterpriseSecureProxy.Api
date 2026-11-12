namespace EnterpriseSecureProxy.Infrastructure.Forwarding;

/// <summary>Per-request options propagated to Polly policies on the named forwarder <see cref="HttpClient"/>.</summary>
internal static class ForwarderHttpRequestOptions
{
    public static readonly HttpRequestOptionsKey<bool> AllowHttpRetry =
        new("EnterpriseSecureProxy.AllowHttpRetry");
}
