namespace EnterpriseSecureProxy.Infrastructure.Persistence.Entities;

/// <summary>EF row for a proxy operation and its parameter/header rules.</summary>
public sealed class ProxyOperationEntity
{
    public Guid Id { get; set; }

    public string OperationKey { get; set; } = string.Empty;

    public Guid DestinationId { get; set; }

    public DestinationEntity Destination { get; set; } = null!;

    public string HttpMethod { get; set; } = "POST";

    public string RelativePath { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public int MaxRequestSize { get; set; } = Domain.Entities.ProxyOperation.DefaultMaxRequestSizeBytes;

    public int? TimeoutMilliseconds { get; set; }

    public bool RequiresEncryption { get; set; }

    public bool AllowUnknownParameters { get; set; }

    public bool AllowHttpClientRetryForDelete { get; set; }

    public string AllowedRoles { get; set; } = string.Empty;

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<ParameterRuleEntity> ParameterRules { get; set; } = new List<ParameterRuleEntity>();

    public ICollection<HeaderRuleEntity> HeaderRules { get; set; } = new List<HeaderRuleEntity>();
}
