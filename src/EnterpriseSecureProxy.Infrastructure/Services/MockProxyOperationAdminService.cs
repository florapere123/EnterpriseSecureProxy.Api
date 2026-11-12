using System.Collections.Concurrent;
using EnterpriseSecureProxy.Application.Dtos;
using EnterpriseSecureProxy.Application.Interfaces;
using EnterpriseSecureProxy.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseSecureProxy.Infrastructure.Services;

/// <summary>Admin updates for mock data — in-memory row versions over seeded configuration.</summary>
public sealed class MockProxyOperationAdminService : IProxyOperationAdminService
{
    private readonly IConfigurationRepository _configuration;
    private readonly ConcurrentDictionary<Guid, byte[]> _rowVersions = new();

    public MockProxyOperationAdminService(IConfigurationRepository configuration) => _configuration = configuration;

    public async Task<ProxyOperationAdminViewDto?> GetOperationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var operations = await _configuration.GetAllOperationsAsync(cancellationToken);
        var match = operations.FirstOrDefault(o => o.Id == id);
        return match is null ? null : ToViewDto(match);
    }

    public Task<UpdateProxyOperationResult> UpdateOperationAsync(
        Guid id,
        UpdateProxyOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        var parseError = MockAdminRowVersion.ValidateRowVersionOrGetError(request.RowVersion);
        if (parseError is not null)
        {
            return Task.FromResult(new UpdateProxyOperationResult { Success = false, Message = parseError });
        }

        var supplied = Convert.FromBase64String(request.RowVersion!);
        MockAdminRowVersion.AssertCurrentOrThrow(_rowVersions, id, supplied);
        MockAdminRowVersion.Bump(_rowVersions, id);

        return Task.FromResult(new UpdateProxyOperationResult
        {
            Success = true,
            OperationKey = id.ToString(),
            Message = "Operation updated (mock store)."
        });
    }

    private ProxyOperationAdminViewDto ToViewDto(Domain.Entities.ProxyOperation operation)
    {
        var version = _rowVersions.GetOrAdd(operation.Id, _ => MockAdminRowVersion.CreateVersionBytes());
        return new ProxyOperationAdminViewDto
        {
            Id = operation.Id,
            OperationKey = operation.OperationKey,
            HttpMethod = operation.Method.ToString().ToUpperInvariant(),
            RelativePath = operation.RelativePath,
            IsActive = operation.IsEnabled,
            MaxRequestSize = operation.MaxRequestSize,
            RequiresEncryption = operation.RequiresEncryption,
            AllowedRoles = string.Join(',', operation.AllowedRoles),
            RowVersion = Convert.ToBase64String(version)
        };
    }
}
