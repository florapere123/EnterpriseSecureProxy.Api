namespace EnterpriseSecureProxy.Infrastructure.Services;

/// <summary>Snapshot of an operation's fields written to change history.</summary>
internal sealed record OperationChangeSummary(
    string OperationKey,
    string HttpMethod,
    string RelativePath,
    bool IsActive,
    int MaxRequestSize,
    bool RequiresEncryption,
    string AllowedRoles);
