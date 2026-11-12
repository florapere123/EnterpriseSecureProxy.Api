using EnterpriseSecureProxy.Infrastructure.Persistence;
using EnterpriseSecureProxy.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnterpriseSecureProxy.Infrastructure.Services;

/// <summary>Shared EF update flow: apply changes, save, write history, and 409 on a stale RowVersion.</summary>
public sealed class OptimisticConcurrencyUpdater
{
    public RowVersionParseResult TryParseRowVersion(string? rowVersionBase64) => RowVersionParser.TryParse(rowVersionBase64);

    public async Task SaveWithHistoryAsync<TEntity>(
        ProxyDbContext db,
        TEntity entity,
        byte[] originalRowVersion,
        Action<TEntity> applyChanges,
        Func<TEntity, byte[], ConfigurationHistoryEntity> createHistory,
        ILogger logger,
        string conflictLogMessage,
        object logId,
        CancellationToken cancellationToken) where TEntity : class
    {
        applyChanges(entity);

        var rowVersion = db.Entry(entity).Property("RowVersion");
        rowVersion.OriginalValue = originalRowVersion;
        // SQL Server rowversion/timestamp is database-generated only — never send it in UPDATE SET.
        rowVersion.IsModified = false;

        // SqlServerRetryingExecutionStrategy requires user transactions to run inside CreateExecutionStrategy.
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await db.SaveChangesAsync(cancellationToken);

                var history = createHistory(entity, originalRowVersion);
                db.ConfigurationHistory.Add(history);
                await db.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await tx.RollbackAsync(cancellationToken);
                logger.LogWarning(ex, conflictLogMessage, logId);
                throw;
            }
        });
    }
}
