using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EnterpriseSecureProxy.Application.Auditing;
using EnterpriseSecureProxy.Application.Dtos;
using EnterpriseSecureProxy.Application.Interfaces;
using EnterpriseSecureProxy.Application.UseCases;
using EnterpriseSecureProxy.Domain.Entities;
using EnterpriseSecureProxy.Domain.Enums;

namespace EnterpriseSecureProxy.Application.Steps;

/// <summary>Replays or caches POST responses by Idempotency-Key. Called directly by the handler, not a pipeline.</summary>
public sealed class IdempotencyStep
{
    private readonly IIdempotencyResponseCache _cache;
    private readonly ProxyAuditRecorder _audit;

    public IdempotencyStep(IIdempotencyResponseCache cache, ProxyAuditRecorder audit)
    {
        _cache = cache;
        _audit = audit;
    }

    public ProxyExecuteResponse? TryGetCachedResponse(
        ProxyOperation operation,
        ProxyInvocationContext context,
        string operationKey,
        ProxyExecuteRequest request,
        long elapsedMs)
    {
        if (operation.Method != HttpVerb.Post || string.IsNullOrWhiteSpace(context.IdempotencyKey))
        {
            return null;
        }

        if (!_cache.TryGet(operationKey, context.IdempotencyKey, out var cached) || cached is null)
        {
            return null;
        }

        // Standard idempotency-key semantics: a key may only be replayed for the same body. Replaying it
        // with a different body is a client error, so we reject it instead of returning the wrong response.
        var incomingBodyHash = ComputeBodyHash(request.Body);
        if (!string.Equals(cached.RequestBodyHash, incomingBodyHash, StringComparison.Ordinal))
        {
            _ = WriteKeyReuseRejectionAuditAsync(context, operationKey, elapsedMs);
            return ProxyExecuteResponse.Failure(
                context.CorrelationId, operationKey, "IDEMPOTENCY_KEY_REUSED_WITH_DIFFERENT_BODY",
                "The Idempotency-Key was already used with a different request body.");
        }

        _ = WriteIdempotentReplayAuditAsync(context, operationKey, cached.StatusCode, elapsedMs);

        return ProxyExecuteResponse.Ok(context.CorrelationId, operationKey, cached.StatusCode, cached.Body);
    }

    public void TryStore(ProxyOperation operation, ProxyInvocationContext context, string operationKey, ProxyExecuteRequest request, ForwardResult result)
    {
        if (operation.Method != HttpVerb.Post || string.IsNullOrWhiteSpace(context.IdempotencyKey))
        {
            return;
        }

        if (!ShouldCache(result))
        {
            return;
        }

        _cache.Store(operationKey, context.IdempotencyKey, new IdempotentDownstreamResponse
        {
            StatusCode = result.StatusCode,
            Body = result.Body,
            RequestBodyHash = ComputeBodyHash(request.Body)
        });
    }

    // SHA-256 over the raw body bytes; same body (byte-for-byte) replays, any difference is treated as reuse.
    private static string ComputeBodyHash(JsonElement? body)
    {
        var raw = body.HasValue ? body.Value.GetRawText() : string.Empty;
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }

    private static bool ShouldCache(ForwardResult result) =>
        result.IsSuccess ||
        (result.FailureReason?.StartsWith("Downstream service returned", StringComparison.Ordinal) ?? false);

    private Task WriteIdempotentReplayAuditAsync(
        ProxyInvocationContext context,
        string operationKey,
        int statusCode,
        long elapsedMs) =>
        _audit.RecordAsync(
            context,
            operationKey,
            AuditOutcome.ServedFromIdempotencyCache,
            elapsedMs,
            statusCode,
            detail: "Served from idempotency cache.");

    private Task WriteKeyReuseRejectionAuditAsync(
        ProxyInvocationContext context,
        string operationKey,
        long elapsedMs) =>
        _audit.RecordAsync(
            context,
            operationKey,
            AuditOutcome.IdempotencyKeyReused,
            elapsedMs,
            detail: "Idempotency-Key reused with a different request body.");
}
