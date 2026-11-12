namespace EnterpriseSecureProxy.Application.Dtos;

/// <summary>Fields an admin can change on a destination, plus the RowVersion for concurrency.</summary>
public sealed class UpdateDestinationRequest
{
    public string? BaseUrl { get; init; }

    public bool? IsActive { get; init; }

    public int? TimeoutMilliseconds { get; init; }

    public string? SettingsJson { get; init; }

    public required string RowVersion { get; init; }
}
