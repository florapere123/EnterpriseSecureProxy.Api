using EnterpriseSecureProxy.Application.Dtos;

namespace EnterpriseSecureProxy.Application.Interfaces;

/// <summary>Admin reads and concurrency-checked updates for proxy operations.</summary>
public interface IProxyOperationAdminService
{
    Task<ProxyOperationAdminViewDto?> GetOperationAsync(Guid id, CancellationToken cancellationToken = default);

    Task<UpdateProxyOperationResult> UpdateOperationAsync(
        Guid id,
        UpdateProxyOperationRequest request,
        CancellationToken cancellationToken = default);
}
