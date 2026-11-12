using EnterpriseSecureProxy.Domain.Entities;
using EnterpriseSecureProxy.Domain.Interfaces;

namespace EnterpriseSecureProxy.Tests.Support;

public sealed class CaptureAuditSink : IAuditSink
{
    public List<AuditLog> Entries { get; } = new();

    public Task RecordAsync(AuditLog entry, CancellationToken cancellationToken = default)
    {
        Entries.Add(entry);
        return Task.CompletedTask;
    }
}
