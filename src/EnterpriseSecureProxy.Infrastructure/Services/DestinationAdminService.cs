using EnterpriseSecureProxy.Application.Dtos;
using EnterpriseSecureProxy.Application.Interfaces;
using EnterpriseSecureProxy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnterpriseSecureProxy.Infrastructure.Services;

/// <summary>SQL-backed reads and concurrency-checked updates for destinations.</summary>
public sealed class DestinationAdminService : IDestinationAdminService
{
    private readonly IDbContextFactory<ProxyDbContext> _dbContextFactory;
    private readonly IConfigurationCacheInvalidator _cacheInvalidator;
    private readonly OptimisticConcurrencyUpdater _concurrency;
    private readonly ILogger<DestinationAdminService> _logger;

    public DestinationAdminService(
        IDbContextFactory<ProxyDbContext> dbContextFactory,
        IConfigurationCacheInvalidator cacheInvalidator,
        OptimisticConcurrencyUpdater concurrency,
        ILogger<DestinationAdminService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _cacheInvalidator = cacheInvalidator;
        _concurrency = concurrency;
        _logger = logger;
    }

    public async Task<DestinationAdminViewDto?> GetDestinationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.Destinations.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        return entity is null ? null : ToViewDto(entity);
    }

    /// <summary>Same RowVersion/409 pattern for destination URL and flags (OWASP Insecure Design).</summary>
    public async Task<UpdateDestinationResult> UpdateDestinationAsync(
        Guid id,
        UpdateDestinationRequest request,
        CancellationToken cancellationToken = default)
    {
        var parsed = _concurrency.TryParseRowVersion(request.RowVersion);
        if (!parsed.Success)
        {
            return new UpdateDestinationResult { Success = false, Message = parsed.ErrorMessage };
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var entity = await db.Destinations.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (entity is null)
        {
            return new UpdateDestinationResult { Success = false, Message = "Destination not found." };
        }

        try
        {
            await _concurrency.SaveWithHistoryAsync(
                db,
                entity,
                parsed.Bytes!,
                ApplyDestinationPatch,
                (e, previousRowVersion) => ConfigurationHistoryWriter.CreateDestinationHistory(
                    e.Id,
                    changedBy: null,
                    new DestinationChangeSummary(
                        e.BaseUrl,
                        e.IsActive,
                        e.TimeoutMilliseconds,
                        request.SettingsJson is null ? null : "SettingsJson"),
                    previousRowVersion,
                    e.RowVersion),
                _logger,
                "Concurrency conflict updating destination {DestinationId}.",
                id,
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new UpdateDestinationResult
            {
                Success = false,
                ConcurrencyConflict = true,
                Message = "The resource was modified by another process. Refresh and retry."
            };
        }

        _cacheInvalidator.EvictDestination(entity.Name);
        _cacheInvalidator.EvictAllOperations();

        return new UpdateDestinationResult
        {
            Success = true,
            DestinationName = entity.Name,
            Message = "Destination updated."
        };

        void ApplyDestinationPatch(Persistence.Entities.DestinationEntity e)
        {
            if (request.BaseUrl is not null) e.BaseUrl = request.BaseUrl;
            if (request.IsActive is not null) e.IsActive = request.IsActive.Value;
            if (request.TimeoutMilliseconds is not null) e.TimeoutMilliseconds = request.TimeoutMilliseconds.Value;
            if (request.SettingsJson is not null) e.SettingsJson = request.SettingsJson;
        }
    }

    private static DestinationAdminViewDto ToViewDto(Persistence.Entities.DestinationEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        BaseUrl = entity.BaseUrl,
        IsActive = entity.IsActive,
        TimeoutMilliseconds = entity.TimeoutMilliseconds,
        SettingsJson = entity.SettingsJson,
        RowVersion = Convert.ToBase64String(entity.RowVersion)
    };
}
