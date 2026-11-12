using EnterpriseSecureProxy.Domain.Entities;
using EnterpriseSecureProxy.Domain.Audit;
using EnterpriseSecureProxy.Domain.Enums;
using EnterpriseSecureProxy.Infrastructure.Persistence;
using EnterpriseSecureProxy.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnterpriseSecureProxy.Infrastructure.Auditing;

/// <summary>Drains queued audit logs in batches and writes them to the store on a background thread.</summary>
public sealed class AuditLogProcessor : BackgroundService
{
    private const int BatchSize = 50;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(1);

    private readonly AuditLogChannel _channel;
    private readonly IDbContextFactory<ProxyDbContext> _dbContextFactory;
    private readonly ILogger<AuditLogProcessor> _logger;

    public AuditLogProcessor(
        AuditLogChannel channel,
        IDbContextFactory<ProxyDbContext> dbContextFactory,
        ILogger<AuditLogProcessor> logger)
    {
        _channel = channel;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batch = new List<AuditLog>(BatchSize);
        using var timer = new PeriodicTimer(FlushInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                DrainChannel(batch);
                if (batch.Count == 0) continue;

                await PersistBatchAsync(batch, stoppingToken);
                batch.Clear();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            DrainChannel(batch);
            if (batch.Count > 0)
            {
                await PersistBatchAsync(batch, CancellationToken.None);
            }
        }
    }

    private void DrainChannel(List<AuditLog> batch)
    {
        while (batch.Count < BatchSize && _channel.Reader.TryRead(out var entry))
        {
            batch.Add(entry);
        }
    }

    private async Task PersistBatchAsync(List<AuditLog> batch, CancellationToken cancellationToken)
    {
        if (batch.Count == 0) return;

        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var entities = batch.Select(ToEntity).ToList();
            db.AuditLogs.AddRange(entities);
            await db.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Persisted {Count} audit records.", entities.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to persist {Count} audit records (SQL unavailable or slow). Dropping batch to protect proxy availability.",
                batch.Count);
        }
    }

    // OWASP Security Logging: persist metadata only—never Authorization, keys, or request/response bodies in the audit row.
    private static AuditLogEntity ToEntity(AuditLog entry)
    {
        var end = entry.TimestampUtc;
        var start = end.AddMilliseconds(-entry.DurationMilliseconds);

        return new AuditLogEntity
        {
            Id = entry.Id,
            CorrelationId = entry.CorrelationId,
            OperationKey = entry.OperationKey,
            StartTime = start.UtcDateTime,
            EndTime = end.UtcDateTime,
            DurationMs = (int)Math.Clamp(entry.DurationMilliseconds, 0, int.MaxValue),
            Forwarded = AuditForwardingRules.WasForwardedToDestination(entry.Outcome),
            UpstreamStatusCode = entry.DownstreamStatusCode,
            OutcomeCategory = AuditOutcomeMapper.ToOutcomeCategory(entry.Outcome),
            RejectionReason = entry.Detail
        };
    }
}