using EnterpriseSecureProxy.Domain.Enums;

namespace EnterpriseSecureProxy.Domain.Entities;

/// <summary>
/// Immutable audit record for every proxy invocation attempt (request log).
/// Captures who invoked what, against which operation, and with what outcome—without payload contents.
/// </summary>
public sealed class AuditLog
{
    public Guid Id { get; init; }

    public string CorrelationId { get; init; } = string.Empty;

    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;

    public string OperationKey { get; init; } = string.Empty;

    public string PrincipalId { get; init; } = "anonymous";

    public string? SourceAddress { get; init; }

    public AuditOutcome Outcome { get; init; }

    public int? DownstreamStatusCode { get; init; }

    public long DurationMilliseconds { get; init; }

    public string? Detail { get; init; }

    public static AuditLog Create(
        string correlationId,
        string operationKey,
        AuditOutcome outcome,
        long durationMilliseconds,
        string? principalId = null,
        string? sourceAddress = null,
        int? downstreamStatusCode = null,
        string? detail = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationKey);

        return new AuditLog
        {
            Id = Guid.NewGuid(),
            CorrelationId = correlationId,
            TimestampUtc = DateTimeOffset.UtcNow,
            OperationKey = operationKey,
            PrincipalId = string.IsNullOrWhiteSpace(principalId) ? "anonymous" : principalId,
            SourceAddress = sourceAddress,
            Outcome = outcome,
            DownstreamStatusCode = downstreamStatusCode,
            DurationMilliseconds = durationMilliseconds,
            Detail = detail
        };
    }

    /// <summary>Alias for operational terminology: request log entry.</summary>
    public static AuditLog CreateRequestLog(
        string correlationId,
        string operationKey,
        AuditOutcome outcome,
        long durationMilliseconds,
        string? principalId = null,
        string? sourceAddress = null,
        int? downstreamStatusCode = null,
        string? detail = null) =>
        Create(correlationId, operationKey, outcome, durationMilliseconds, principalId, sourceAddress, downstreamStatusCode, detail);
}
