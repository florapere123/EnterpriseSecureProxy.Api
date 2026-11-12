namespace EnterpriseSecureProxy.Application.Interfaces;

/// <summary>
/// Evicts cached proxy configuration after administrative changes (strict consistency).
/// </summary>
public interface IConfigurationCacheInvalidator
{
    void EvictOperation(string operationKey);

    void EvictDestination(string destinationName);

    void EvictAllOperations();
}