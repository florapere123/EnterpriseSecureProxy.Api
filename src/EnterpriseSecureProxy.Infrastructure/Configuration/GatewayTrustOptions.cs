namespace EnterpriseSecureProxy.Infrastructure.Configuration;

public sealed class GatewayTrustOptions
{
    public const string SectionName = "GatewayTrust";

    /// <summary>
    /// When true, the proxy accepts <c>X-Gateway-Asserted-Role</c> from an upstream gateway/mTLS terminator.
    /// Must remain false when the API is exposed directly to untrusted callers.
    /// </summary>
    public bool TrustGatewayHeaders { get; set; }
}
