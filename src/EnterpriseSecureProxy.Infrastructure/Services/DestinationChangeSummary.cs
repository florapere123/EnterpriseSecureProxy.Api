namespace EnterpriseSecureProxy.Infrastructure.Services;

/// <summary>Snapshot of a destination's fields written to change history.</summary>
internal sealed record DestinationChangeSummary(
    string BaseUrl,
    bool IsActive,
    int TimeoutMilliseconds,
    string? SettingsKeyReference);
