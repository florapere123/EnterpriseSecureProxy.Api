using EnterpriseSecureProxy.Application.Interfaces;
using EnterpriseSecureProxy.Domain.Interfaces;
using EnterpriseSecureProxy.Domain.Entities;
using EnterpriseSecureProxy.Infrastructure.Configuration;
using EnterpriseSecureProxy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseSecureProxy.Infrastructure.Repositories;

/// <summary>
/// EF Core-backed configuration store with in-memory caching for hot proxy operation lookups.
/// </summary>
public sealed class SqlConfigurationRepository : IConfigurationRepository, IConfigurationCacheInvalidator
{
    private const string CachePrefix = "proxy:config:";

    public const string AllOperationsCacheKey = CachePrefix + "operations:all";
    public static string OperationCacheKey(string operationKey) => $"{CachePrefix}operation:{operationKey}";
    public static string DestinationCacheKey(string destinationName) => $"{CachePrefix}destination:{destinationName}";

    private readonly TimeSpan _cacheDuration;
    private readonly IDbContextFactory<ProxyDbContext> _dbContextFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SqlConfigurationRepository> _logger;

    public SqlConfigurationRepository(
        IDbContextFactory<ProxyDbContext> dbContextFactory,
        IMemoryCache cache,
        IOptions<ProxyDefaultsOptions> defaults,
        ILogger<SqlConfigurationRepository> logger)
    {
        _dbContextFactory = dbContextFactory;
        _cache = cache;
        _cacheDuration = TimeSpan.FromMinutes(defaults.Value.ConfigCacheMinutes);
        _logger = logger;
    }

    public async Task<ProxyOperation?> GetOperationAsync(string operationKey, CancellationToken cancellationToken = default)
    {
        var cacheKey = OperationCacheKey(operationKey);
        if (_cache.TryGetValue(cacheKey, out ProxyOperation? cached))
        {
            return cached;
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var entity = await db.ProxyOperations
            .AsNoTracking()
            .Include(o => o.Destination)
            .Include(o => o.ParameterRules)
            .Include(o => o.HeaderRules)
            .FirstOrDefaultAsync(o => o.OperationKey == operationKey && o.IsActive, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var operation = ConfigurationEntityMapper.ToDomain(entity);
        _cache.Set(cacheKey, operation, _cacheDuration);
        return operation;
    }

    public async Task<Destination?> GetDestinationAsync(string destinationName, CancellationToken cancellationToken = default)
    {
        var cacheKey = DestinationCacheKey(destinationName);
        if (_cache.TryGetValue(cacheKey, out Destination? cached))
        {
            return cached;
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var entity = await db.Destinations
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Name == destinationName && d.IsActive, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var destination = ConfigurationEntityMapper.ToDomain(entity);
        _cache.Set(cacheKey, destination, _cacheDuration);
        return destination;
    }

    public async Task<IReadOnlyList<ProxyOperation>> GetAllOperationsAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(AllOperationsCacheKey, out IReadOnlyList<ProxyOperation>? cached) && cached is not null)
        {
            return cached;
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var entities = await db.ProxyOperations
            .AsNoTracking()
            .Include(o => o.Destination)
            .Include(o => o.ParameterRules)
            .Include(o => o.HeaderRules)
            .OrderBy(o => o.OperationKey)
            .ToListAsync(cancellationToken);

        var operations = entities.Select(ConfigurationEntityMapper.ToDomain).ToList();
        _cache.Set(AllOperationsCacheKey, operations, _cacheDuration);

        _logger.LogDebug("Loaded {Count} proxy operations from SQL configuration store.", operations.Count);

        return operations;
    }

    public void EvictOperation(string operationKey)
    {
        _cache.Remove(OperationCacheKey(operationKey));
        _cache.Remove(AllOperationsCacheKey);
        _logger.LogInformation("Evicted cached configuration for operation '{OperationKey}'.", operationKey);
    }

    public void EvictDestination(string destinationName)
    {
        _cache.Remove(DestinationCacheKey(destinationName));
        _logger.LogInformation("Evicted cached configuration for destination '{DestinationName}'.", destinationName);
    }

    public void EvictAllOperations() => _cache.Remove(AllOperationsCacheKey);
}
