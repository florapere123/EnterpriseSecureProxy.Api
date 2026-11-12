namespace EnterpriseSecureProxy.Infrastructure.Configuration;

public sealed class DownstreamOptions
{
    public const string SectionName = "Downstream";

    /// <summary>When true, outbound HttpClient calls are handled by <see cref="Forwarding.MockDownstreamHttpMessageHandler"/>.</summary>
    public bool UseMockHandler { get; set; }

    /// <summary>Maximum simultaneous in-flight outbound calls per destination name.</summary>
    public int MaxConcurrentRequestsPerDestination { get; set; } = 32;
}
