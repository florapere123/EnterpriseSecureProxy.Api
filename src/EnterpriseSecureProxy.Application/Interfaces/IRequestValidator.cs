using EnterpriseSecureProxy.Application.Dtos;
using EnterpriseSecureProxy.Domain.Entities;
using EnterpriseSecureProxy.Domain.Exceptions;

namespace EnterpriseSecureProxy.Application.Interfaces;

/// <summary>
/// Validates an inbound request against the <see cref="ParameterRule"/>s of a resolved operation.
/// Returns the collected <see cref="ValidationError"/>s; an empty list means the request is valid.
/// </summary>
public interface IRequestValidator
{
    IReadOnlyList<ValidationError> Validate(ProxyOperation operation, ProxyExecuteRequest request);
}
