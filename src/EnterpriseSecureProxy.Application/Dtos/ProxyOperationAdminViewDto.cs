namespace EnterpriseSecureProxy.Application.Dtos;

/// <summary>Operation as shown in the admin API, including the current RowVersion.</summary>
public sealed class ProxyOperationAdminViewDto
{
    public Guid Id { get; init; }
    public string OperationKey { get; init; } = string.Empty;
    public string HttpMethod { get; init; } = string.Empty;
    public string RelativePath { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int MaxRequestSize { get; init; }
    public bool RequiresEncryption { get; init; }
    public string AllowedRoles { get; init; } = string.Empty;
    public string RowVersion { get; init; } = string.Empty;
}
