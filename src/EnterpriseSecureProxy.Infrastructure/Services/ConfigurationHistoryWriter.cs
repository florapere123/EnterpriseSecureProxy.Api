using System.Text.Json;
using EnterpriseSecureProxy.Infrastructure.Persistence.Entities;

namespace EnterpriseSecureProxy.Infrastructure.Services;

/// <summary>Persist config change history without secrets (OWASP Security Logging).</summary>
internal static class ConfigurationHistoryWriter
{
    public static ConfigurationHistoryEntity CreateOperationHistory(
        Guid operationId,
        string? changedBy,
        object changeSummary,
        byte[]? previousRowVersion,
        byte[]? newRowVersion) =>
        new()
        {
            Id = Guid.NewGuid(),
            EntityType = "ProxyOperation",
            EntityId = operationId,
            ChangedAtUtc = DateTime.UtcNow,
            ChangedBy = changedBy,
            ChangeSummaryJson = JsonSerializer.Serialize(changeSummary),
            PreviousRowVersion = previousRowVersion,
            NewRowVersion = newRowVersion
        };

    public static ConfigurationHistoryEntity CreateDestinationHistory(
        Guid destinationId,
        string? changedBy,
        object changeSummary,
        byte[]? previousRowVersion,
        byte[]? newRowVersion) =>
        new()
        {
            Id = Guid.NewGuid(),
            EntityType = "Destination",
            EntityId = destinationId,
            ChangedAtUtc = DateTime.UtcNow,
            ChangedBy = changedBy,
            ChangeSummaryJson = JsonSerializer.Serialize(changeSummary),
            PreviousRowVersion = previousRowVersion,
            NewRowVersion = newRowVersion
        };
}
