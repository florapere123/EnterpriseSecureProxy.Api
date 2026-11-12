namespace EnterpriseSecureProxy.Application.Dtos;

/// <summary>Destination as shown in the admin API, including the current RowVersion.</summary>
public sealed class DestinationAdminViewDto
{
    public Guid Id { get; init; }

    public required string Name { get; init; }

    public required string BaseUrl { get; init; }

    public bool IsActive { get; init; }

    public int TimeoutMilliseconds { get; init; }

    public string? SettingsJson { get; init; }

    public required string RowVersion { get; init; }
}
