using EnterpriseSecureProxy.Application.Interfaces;

namespace EnterpriseSecureProxy.Infrastructure.Configuration;

/// <summary>No-op cache invalidator for the mock data source, which has no cache to evict.</summary>
internal sealed class NullConfigurationCacheInvalidator : IConfigurationCacheInvalidator
{
    public void EvictOperation(string operationKey) { }

    public void EvictDestination(string destinationName) { }

    public void EvictAllOperations() { }
}
