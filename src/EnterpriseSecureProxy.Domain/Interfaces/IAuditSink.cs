using EnterpriseSecureProxy.Domain.Entities;

namespace EnterpriseSecureProxy.Domain.Interfaces;

/// <summary>Append-only sink for the security audit trail. Implementations must not leak failures to callers.</summary>
public interface IAuditSink
{
    Task RecordAsync(AuditLog entry, CancellationToken cancellationToken = default);
}
