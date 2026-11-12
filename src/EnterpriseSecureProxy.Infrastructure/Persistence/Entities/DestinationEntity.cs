namespace EnterpriseSecureProxy.Infrastructure.Persistence.Entities;

/// <summary>EF row for a downstream destination.</summary>
public sealed class DestinationEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public int TimeoutMilliseconds { get; set; } = 30_000;

    public string? SettingsJson { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<ProxyOperationEntity> Operations { get; set; } = new List<ProxyOperationEntity>();
}
