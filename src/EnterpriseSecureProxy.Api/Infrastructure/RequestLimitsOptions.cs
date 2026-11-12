namespace EnterpriseSecureProxy.Api.Infrastructure;

/// <summary>
/// Absolute, operation-independent request-size floor applied before any per-operation logic runs.
/// Mirrors the Kestrel body-size limit so oversized payloads are rejected before full buffering.
/// </summary>
public sealed class RequestLimitsOptions
{
    public const string SectionName = "RequestLimits";

    /// <summary>Hard upper bound on any inbound request body, 
    /// in bytes (default 5 MB).</summary>
    public long MaxGlobalBodyBytes { get; set; } = 5 * 1024 * 1024;

    /// <summary>Maximum bytes read while peeking for the operation key before the per-operation limit is known.</summary>
    public int OperationKeyPeekBytes { get; set; } = 32 * 1024;
}
