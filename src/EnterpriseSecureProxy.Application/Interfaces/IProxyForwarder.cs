using EnterpriseSecureProxy.Application.Dtos;

namespace EnterpriseSecureProxy.Application.Interfaces;

/// <summary>
/// Forwards a fully-validated <see cref="ForwardRequest"/> to the internal microservice and
/// returns a normalized <see cref="ForwardResult"/>. Resilience concerns (timeouts, retries,
/// circuit breaking) live inside the implementation, keeping use cases transport-agnostic.
/// </summary>
public interface IProxyForwarder
{
    Task<ForwardResult> ForwardAsync(ForwardRequest request, CancellationToken cancellationToken = default);
}
