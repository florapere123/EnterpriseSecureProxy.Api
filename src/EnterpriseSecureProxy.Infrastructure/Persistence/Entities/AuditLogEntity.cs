namespace EnterpriseSecureProxy.Infrastructure.Persistence.Entities;

/// <summary>
/// Persisted audit metadata for proxy invocations. Does not store operational payloads.
/// </summary>
public sealed class AuditLogEntity
{
    public Guid Id { get; set; }

    public string CorrelationId { get; set; } = string.Empty;

    public string OperationKey { get; set; } = string.Empty;

    public Guid? DestinationId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public int DurationMs { get; set; }

    public bool Forwarded { get; set; }

    public int? UpstreamStatusCode { get; set; }

    public int? ProxyStatusCode { get; set; }

    public string OutcomeCategory { get; set; } = string.Empty;

    public string? RejectionReason { get; set; }
}
