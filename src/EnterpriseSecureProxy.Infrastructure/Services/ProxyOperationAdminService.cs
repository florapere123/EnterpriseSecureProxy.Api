using EnterpriseSecureProxy.Application.Dtos;
using EnterpriseSecureProxy.Application.Interfaces;
using EnterpriseSecureProxy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnterpriseSecureProxy.Infrastructure.Services;

/// <summary>SQL-backed reads and concurrency-checked updates for proxy operations.</summary>
public sealed class ProxyOperationAdminService : IProxyOperationAdminService
{
    private readonly IDbContextFactory<ProxyDbContext> _dbContextFactory;
    private readonly IConfigurationCacheInvalidator _cacheInvalidator;
    private readonly OptimisticConcurrencyUpdater _concurrency;
    private readonly ILogger<ProxyOperationAdminService> _logger;

    public ProxyOperationAdminService(
        IDbContextFactory<ProxyDbContext> dbContextFactory,
        IConfigurationCacheInvalidator cacheInvalidator,
        OptimisticConcurrencyUpdater concurrency,
        ILogger<ProxyOperationAdminService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _cacheInvalidator = cacheInvalidator;
        _concurrency = concurrency;
        _logger = logger;
    }

    public async Task<ProxyOperationAdminViewDto?> GetOperationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var entity = await db.ProxyOperations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        return entity is null ? null : ToViewDto(entity);
    }

    /// <summary>Stale RowVersion yields 409 so admins do not overwrite each other's changes (OWASP Insecure Design).</summary>
    public async Task<UpdateProxyOperationResult> UpdateOperationAsync(
        Guid id,
        UpdateProxyOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        var parsed = _concurrency.TryParseRowVersion(request.RowVersion);
        if (!parsed.Success)
        {
            return new UpdateProxyOperationResult { Success = false, Message = parsed.ErrorMessage };
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var entity = await db.ProxyOperations
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (entity is null)
        {
            return new UpdateProxyOperationResult { Success = false, Message = "Operation not found." };
        }

        try
        {
            await _concurrency.SaveWithHistoryAsync(
                db,
                entity,
                parsed.Bytes!,
                ApplyOperationPatch,
                (e, previousRowVersion) => ConfigurationHistoryWriter.CreateOperationHistory(
                    e.Id,
                    changedBy: null,
                    new OperationChangeSummary(
                        e.OperationKey,
                        e.HttpMethod,
                        e.RelativePath,
                        e.IsActive,
                        e.MaxRequestSize,
                        e.RequiresEncryption,
                        e.AllowedRoles),
                    previousRowVersion,
                    e.RowVersion),
                _logger,
                "Concurrency conflict updating proxy operation {OperationId}.",
                id,
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new UpdateProxyOperationResult
            {
                Success = false,
                ConcurrencyConflict = true,
                Message = "The resource was modified by another process. Refresh and retry."
            };
        }

        _cacheInvalidator.EvictOperation(entity.OperationKey);
        _cacheInvalidator.EvictAllOperations();

        return new UpdateProxyOperationResult
        {
            Success = true,
            OperationKey = entity.OperationKey,
            Message = "Operation updated."
        };

        void ApplyOperationPatch(Persistence.Entities.ProxyOperationEntity e)
        {
            if (request.RelativePath is not null) e.RelativePath = request.RelativePath;
            if (request.HttpMethod is not null) e.HttpMethod = request.HttpMethod;
            if (request.IsActive is not null) e.IsActive = request.IsActive.Value;
            if (request.MaxRequestSize is not null) e.MaxRequestSize = request.MaxRequestSize.Value;
            if (request.RequiresEncryption is not null) e.RequiresEncryption = request.RequiresEncryption.Value;
            if (request.AllowedRoles is not null) e.AllowedRoles = request.AllowedRoles;
        }
    }

    private static ProxyOperationAdminViewDto ToViewDto(Persistence.Entities.ProxyOperationEntity entity) => new()
    {
        Id = entity.Id,
        OperationKey = entity.OperationKey,
        HttpMethod = entity.HttpMethod,
        RelativePath = entity.RelativePath,
        IsActive = entity.IsActive,
        MaxRequestSize = entity.MaxRequestSize,
        RequiresEncryption = entity.RequiresEncryption,
        AllowedRoles = entity.AllowedRoles,
        RowVersion = Convert.ToBase64String(entity.RowVersion)
    };
}
