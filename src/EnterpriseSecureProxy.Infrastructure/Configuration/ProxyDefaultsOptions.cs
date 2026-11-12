namespace EnterpriseSecureProxy.Infrastructure.Configuration;

/// <summary>Operational defaults for mock seed data, config cache, idempotency TTL, and destination timeouts.</summary>
public sealed class ProxyDefaultsOptions
{
    public const string SectionName = "ProxyDefaults";

    public int DefaultMaxRequestSizeBytes { get; set; } = 1_048_576;

    public int ConfigCacheMinutes { get; set; } = 5;

    public int IdempotencyCacheMinutes { get; set; } = 5;

    public int DefaultDestinationTimeoutSeconds { get; set; } = 20;

    /// <summary>Used for mock destinations that need a longer outbound window (e.g. vision-api).</summary>
    public int SlowDestinationTimeoutSeconds { get; set; } = 45;
}
