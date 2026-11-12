namespace EnterpriseSecureProxy.Application.Dtos;

/// <summary>Result of a destination update: success flag and a message.</summary>
public sealed class UpdateDestinationResult
{
    public bool Success { get; init; }

    public bool ConcurrencyConflict { get; init; }

    public string? DestinationName { get; init; }

    public string? Message { get; init; }
}
