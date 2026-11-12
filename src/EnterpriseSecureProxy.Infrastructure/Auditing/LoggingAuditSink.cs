using EnterpriseSecureProxy.Domain.Interfaces;
using EnterpriseSecureProxy.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace EnterpriseSecureProxy.Infrastructure.Auditing;

/// <summary>
/// Stage 1 <see cref="IAuditSink"/> that emits structured audit events to the logging pipeline.
/// A durable, tamper-evident sink (database/SIEM) replaces this in a later stage. The sink never
/// throws into the caller: losing the request because auditing failed is itself a failure mode.
/// </summary>
public sealed class LoggingAuditSink : IAuditSink
{
    private readonly ILogger<LoggingAuditSink> _logger;

    public LoggingAuditSink(ILogger<LoggingAuditSink> logger) => _logger = logger;

    public Task RecordAsync(AuditLog entry, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "AUDIT {Outcome} op={OperationKey} principal={PrincipalId} source={SourceAddress} " +
                "downstream={DownstreamStatusCode} durationMs={DurationMilliseconds} correlation={CorrelationId} detail={Detail}",
                entry.Outcome, entry.OperationKey, entry.PrincipalId, entry.SourceAddress,
                entry.DownstreamStatusCode, entry.DurationMilliseconds, entry.CorrelationId, entry.Detail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit entry for correlation {CorrelationId}.", entry.CorrelationId);
        }

        return Task.CompletedTask;
    }
}
