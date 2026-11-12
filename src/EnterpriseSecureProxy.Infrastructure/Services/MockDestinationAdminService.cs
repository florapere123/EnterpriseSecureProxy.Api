using System.Collections.Concurrent;
using EnterpriseSecureProxy.Application.Dtos;
using EnterpriseSecureProxy.Application.Interfaces;
using EnterpriseSecureProxy.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseSecureProxy.Infrastructure.Services;

/// <summary>Admin reads and updates for destinations over the in-memory mock data.</summary>
public sealed class MockDestinationAdminService : IDestinationAdminService
{
    private readonly IConfigurationRepository _configuration;
    private readonly ConcurrentDictionary<Guid, byte[]> _rowVersions = new();

    public MockDestinationAdminService(IConfigurationRepository configuration) => _configuration = configuration;

    public async Task<DestinationAdminViewDto?> GetDestinationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var operations = await _configuration.GetAllOperationsAsync(cancellationToken);
        foreach (var destinationName in operations.Select(o => o.DestinationName).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var dest = await _configuration.GetDestinationAsync(destinationName, cancellationToken);
            if (dest is null || dest.Id != id)
            {
                continue;
            }

            return new DestinationAdminViewDto
            {
                Id = dest.Id,
                Name = dest.Name,
                BaseUrl = dest.BaseUri.ToString(),
                IsActive = dest.IsEnabled,
                TimeoutMilliseconds = (int)dest.Timeout.TotalMilliseconds,
                SettingsJson = dest.SettingsJson,
                RowVersion = Convert.ToBase64String(_rowVersions.GetOrAdd(dest.Id, _ => MockAdminRowVersion.CreateVersionBytes()))
            };
        }

        return null;
    }

    public Task<UpdateDestinationResult> UpdateDestinationAsync(
        Guid id,
        UpdateDestinationRequest request,
        CancellationToken cancellationToken = default)
    {
        var parseError = MockAdminRowVersion.ValidateRowVersionOrGetError(request.RowVersion);
        if (parseError is not null)
        {
            return Task.FromResult(new UpdateDestinationResult { Success = false, Message = parseError });
        }

        var supplied = Convert.FromBase64String(request.RowVersion!);
        MockAdminRowVersion.AssertCurrentOrThrow(_rowVersions, id, supplied);
        MockAdminRowVersion.Bump(_rowVersions, id);

        return Task.FromResult(new UpdateDestinationResult { Success = true, Message = "Destination updated (mock)." });
    }
}
