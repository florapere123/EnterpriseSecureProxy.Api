namespace EnterpriseSecureProxy.Infrastructure.Persistence.Entities;

public sealed class HeaderRuleEntity
{
    public Guid Id { get; set; }

    public Guid OperationId { get; set; }

    public ProxyOperationEntity Operation { get; set; } = null!;

    public string HeaderName { get; set; } = string.Empty;

    /// <summary>Forward or Inject.</summary>
    public string Mode { get; set; } = "Forward";

    public string? InjectedValue { get; set; }

    public bool IsRequired { get; set; }
}
