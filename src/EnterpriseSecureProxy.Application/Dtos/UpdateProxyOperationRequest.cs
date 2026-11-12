namespace EnterpriseSecureProxy.Application.Dtos;

/// <summary>Administrative update payload for optimistic concurrency on <c>ProxyOperations</c>.</summary>
public sealed class UpdateProxyOperationRequest
{
    public string? RelativePath { get; init; }

    public string? HttpMethod { get; init; }

    public bool? IsActive { get; init; }

    public int? MaxRequestSize { get; init; }

    public bool? RequiresEncryption { get; init; }

    public string? AllowedRoles { get; init; }

    /// <summary>Base64-encoded SQL Server <c>rowversion</c> token from the last read.</summary>
    public string RowVersion { get; init; } = string.Empty;
}

/// <summary>Result of an operation update: success flag, optional conflict flag, and a message.</summary>
public sealed class UpdateProxyOperationResult
{
    public bool Success { get; init; }
    public bool ConcurrencyConflict { get; init; }
    public string? OperationKey { get; init; }
    public string? Message { get; init; }
}