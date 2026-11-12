using EnterpriseSecureProxy.Domain.Entities;

namespace EnterpriseSecureProxy.Domain.Interfaces;

/// <summary>Read-only access to configured proxy destinations and operations.</summary>
public interface IConfigurationRepository
{
    Task<ProxyOperation?> GetOperationAsync(string operationKey, CancellationToken cancellationToken = default);

    Task<Destination?> GetDestinationAsync(string destinationName, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProxyOperation>> GetAllOperationsAsync(CancellationToken cancellationToken = default);
}
