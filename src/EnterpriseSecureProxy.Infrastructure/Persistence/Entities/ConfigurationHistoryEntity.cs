namespace EnterpriseSecureProxy.Infrastructure.Persistence.Entities;

/// <summary>EF row recording one config change: who, when, and a secret-free summary.</summary>
public sealed class ConfigurationHistoryEntity
{
    public Guid Id { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public DateTime ChangedAtUtc { get; set; }

    public string? ChangedBy { get; set; }

    public string ChangeSummaryJson { get; set; } = "{}";

    public byte[]? PreviousRowVersion { get; set; }

    public byte[]? NewRowVersion { get; set; }
}
