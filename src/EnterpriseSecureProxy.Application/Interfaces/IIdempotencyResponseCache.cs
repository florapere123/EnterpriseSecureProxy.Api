using System.Text.Json;

namespace EnterpriseSecureProxy.Application.Interfaces;

/// <summary>A downstream response kept so a repeated Idempotency-Key can replay it.</summary>
public sealed class IdempotentDownstreamResponse
{
    public required int StatusCode { get; init; }
    public JsonElement? Body { get; init; }

    /// <summary>
    /// Hash of the request body that produced this response, so a replayed Idempotency-Key carrying a
    /// different body can be rejected instead of silently returning the wrong cached result.
    /// </summary>
    public string? RequestBodyHash { get; init; }
}

/// <summary>Stores and looks up cached responses keyed by operation and Idempotency-Key.</summary>
public interface IIdempotencyResponseCache
{
    bool TryGet(string operationKey, string idempotencyKey, out IdempotentDownstreamResponse? response);

    void Store(string operationKey, string idempotencyKey, IdempotentDownstreamResponse response);
}
