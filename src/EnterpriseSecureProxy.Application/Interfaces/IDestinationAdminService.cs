using EnterpriseSecureProxy.Application.Dtos;

namespace EnterpriseSecureProxy.Application.Interfaces;

/// <summary>Admin reads and concurrency-checked updates for destinations.</summary>
public interface IDestinationAdminService
{
    Task<DestinationAdminViewDto?> GetDestinationAsync(Guid id, CancellationToken cancellationToken = default);

    Task<UpdateDestinationResult> UpdateDestinationAsync(
        Guid id,
        UpdateDestinationRequest request,
        CancellationToken cancellationToken = default);
}
