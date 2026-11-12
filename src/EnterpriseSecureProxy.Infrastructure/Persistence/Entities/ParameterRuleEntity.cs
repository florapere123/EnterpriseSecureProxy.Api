namespace EnterpriseSecureProxy.Infrastructure.Persistence.Entities;

public sealed class ParameterRuleEntity
{
    public Guid Id { get; set; }

    public Guid OperationId { get; set; }

    public ProxyOperationEntity Operation { get; set; } = null!;

    public string ParameterName { get; set; } = string.Empty;

    public bool IsRequired { get; set; } = true;

    /// <summary>Domain primitive type name, e.g. String, Integer, Guid, Json.</summary>
    public string DataType { get; set; } = "String";

    /// <summary>Path, Query, or Body (mapped to domain ParameterLocation).</summary>
    public string Location { get; set; } = "Body";

    public int? MaxLength { get; set; }

    /// <summary>Comma-separated allowlist values when the rule is enum-like (e.g. MGRS,LatLong).</summary>
    public string? AllowedValues { get; set; }

    /// <summary>Optional regular expression the value must satisfy.</summary>
    public string? Pattern { get; set; }
}
