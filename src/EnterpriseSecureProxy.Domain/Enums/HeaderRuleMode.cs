namespace EnterpriseSecureProxy.Domain.Enums;

/// <summary>How the proxy treats a header when building the outbound request.</summary>
public enum HeaderRuleMode
{
    /// <summary>Header is copied from the inbound request when present (allowlist).</summary>
    Forward = 1,

    /// <summary>Header is injected by the proxy with a fixed value, overwriting any inbound value.</summary>
    Inject = 2,

    /// <summary>Header is explicitly stripped and never forwarded (denylist).</summary>
    Block = 3
}
