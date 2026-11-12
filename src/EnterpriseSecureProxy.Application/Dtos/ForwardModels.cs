using System.Text.Json;
using EnterpriseSecureProxy.Domain.Entities;

namespace EnterpriseSecureProxy.Application.Dtos;

/// <summary>
/// Fully-resolved, validated instruction describing exactly what the proxy should send downstream.
/// Produced by the Application layer and consumed by an <see cref="Interfaces.IProxyForwarder"/>.
/// </summary>
public sealed class ForwardRequest
{
    public required string CorrelationId { get; init; }
    public required ProxyOperation Operation { get; init; }
    public required Destination Destination { get; init; }

    /// <summary>Absolute outbound URI (destination base + operation path + resolved query).</summary>
    public required Uri TargetUri { get; init; }

    /// <summary>Header allowlist already resolved to concrete name/value pairs.</summary>
    public IReadOnlyDictionary<string, string> Headers { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Optional JSON body to forward.</summary>
    public JsonElement? Body { get; init; }
}

/// <summary>Raw result of a downstream call, before it is wrapped into a <see cref="ProxyExecuteResponse"/>.</summary>
public sealed class ForwardResult
{
    public required bool IsSuccess { get; init; }
    public required int StatusCode { get; init; }
    public JsonElement? Body { get; init; }
    public string? FailureReason { get; init; }
}
