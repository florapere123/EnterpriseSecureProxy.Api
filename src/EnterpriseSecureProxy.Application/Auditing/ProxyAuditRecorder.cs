using EnterpriseSecureProxy.Domain.Entities;
using EnterpriseSecureProxy.Domain.Enums;
using EnterpriseSecureProxy.Domain.Interfaces;
using EnterpriseSecureProxy.Application.UseCases;

namespace EnterpriseSecureProxy.Application.Auditing;

/// <summary>Single entry point for proxy invocation audit records (metadata only).</summary>
public sealed class ProxyAuditRecorder
{
    private readonly IAuditSink _sink;

    public ProxyAuditRecorder(IAuditSink sink) => _sink = sink;

    /// <summary>
    /// OWASP Security Logging: log correlation and outcome per attempt; do not store auth headers, keys, or payload content.
    /// </summary>
    public Task RecordAsync(
        ProxyInvocationContext context,
        string operationKey,
        AuditOutcome outcome,
        long durationMilliseconds,
        int? downstreamStatusCode = null,
        string? detail = null,
        CancellationToken cancellationToken = default)
    {
        var entry = AuditLog.Create(
            context.CorrelationId,
            operationKey,
            outcome,
            durationMilliseconds,
            context.PrincipalId,
            context.SourceAddress,
            downstreamStatusCode,
            detail);

        return _sink.RecordAsync(entry, cancellationToken);
    }
}
