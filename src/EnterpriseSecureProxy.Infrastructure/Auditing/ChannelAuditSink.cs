using EnterpriseSecureProxy.Domain.Entities;
using EnterpriseSecureProxy.Domain.Interfaces;

namespace EnterpriseSecureProxy.Infrastructure.Auditing;

/// <summary>Audit sink that hands logs to the background channel instead of writing inline.</summary>
public sealed class ChannelAuditSink : IAuditSink
{
    private readonly AuditLogChannel _channel;

    public ChannelAuditSink(AuditLogChannel channel) => _channel = channel;

    public Task RecordAsync(AuditLog entry, CancellationToken cancellationToken = default)
    {
        try
        {
            _channel.TryWrite(entry);
        }
        catch
        {
            // Audit must never fault the proxy pipeline.
        }

        return Task.CompletedTask;
    }
}