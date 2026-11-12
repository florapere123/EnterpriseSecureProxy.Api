using System.Threading.Channels;
using EnterpriseSecureProxy.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace EnterpriseSecureProxy.Infrastructure.Auditing;

/// <summary>In-memory bounded buffer between the proxy hot path and the asynchronous SQL audit writer.</summary>
public sealed class AuditLogChannel
{
    private readonly Channel<AuditLog> _channel;
    private readonly ILogger<AuditLogChannel> _logger;

    public AuditLogChannel(ILogger<AuditLogChannel> logger)
    {
        _logger = logger;
        _channel = Channel.CreateBounded<AuditLog>(new BoundedChannelOptions(10_000)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public ChannelReader<AuditLog> Reader => _channel.Reader;

    public bool TryWrite(AuditLog entry)
    {
        if (_channel.Writer.TryWrite(entry))
        {
            return true;
        }

        _logger.LogWarning(
            "Audit channel full; dropping audit entry for correlation {CorrelationId}, operation {OperationKey}.",
            entry.CorrelationId, entry.OperationKey);
        return false;
    }
}
