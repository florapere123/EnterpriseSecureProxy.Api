using EnterpriseSecureProxy.Application.Interfaces;
using EnterpriseSecureProxy.Infrastructure.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace EnterpriseSecureProxy.Infrastructure.Caching;

/// <summary>
/// In-process idempotency replay cache. Entries reset on process restart and do not replicate across
/// load-balanced instances; production deployments should use a distributed cache (for example Redis)
/// keyed by <c>Idempotency-Key</c> plus <c>OperationKey</c>.
/// </summary>
public sealed class MemoryIdempotencyResponseCache : IIdempotencyResponseCache
{
    private const string KeyPrefix = "Idempotency:";

    private readonly IMemoryCache _cache;
    private readonly TimeSpan _ttl;

    public MemoryIdempotencyResponseCache(IMemoryCache cache)
        : this(cache, Options.Create(new ProxyDefaultsOptions()))
    {
    }

    public MemoryIdempotencyResponseCache(IMemoryCache cache, IOptions<ProxyDefaultsOptions> defaults)
    {
        _cache = cache;
        _ttl = TimeSpan.FromMinutes(defaults.Value.IdempotencyCacheMinutes);
    }

    public bool TryGet(string operationKey, string idempotencyKey, out IdempotentDownstreamResponse? response)
    {
        var cacheKey = BuildKey(operationKey, idempotencyKey);
        if (_cache.TryGetValue(cacheKey, out IdempotentDownstreamResponse? cached))
        {
            response = cached;
            return true;
        }

        response = null;
        return false;
    }

    public void Store(string operationKey, string idempotencyKey, IdempotentDownstreamResponse response) =>
        _cache.Set(BuildKey(operationKey, idempotencyKey), response, _ttl);

    private static string BuildKey(string operationKey, string idempotencyKey) =>
        $"{KeyPrefix}{operationKey}:{idempotencyKey}";
}
