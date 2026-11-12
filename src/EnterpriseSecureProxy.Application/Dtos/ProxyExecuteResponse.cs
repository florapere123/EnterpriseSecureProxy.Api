using System.Text.Json;
using EnterpriseSecureProxy.Domain.Exceptions;

namespace EnterpriseSecureProxy.Application.Dtos;

/// <summary>
/// Structured, envelope-style response returned by the proxy regardless of success or failure.
/// A stable envelope keeps clients decoupled from downstream service response shapes.
/// </summary>
public sealed class ProxyExecuteResponse
{
    public required bool Success { get; init; }

    /// <summary>Correlation id echoed back for tracing against logs and the audit trail.</summary>
    public required string CorrelationId { get; init; }

    /// <summary>Resolved operation key that was executed (or attempted).</summary>
    public required string OperationKey { get; init; }

    /// <summary>Machine-readable status token, e.g. "OK", "NOT_FOUND", "VALIDATION_FAILED".</summary>
    public required string Status { get; init; }

    /// <summary>HTTP status code returned by the downstream service, when the call was made.</summary>
    public int? DownstreamStatusCode { get; init; }

    /// <summary>Downstream response body, surfaced verbatim on success.</summary>
    public JsonElement? Data { get; init; }

    /// <summary>Validation errors, populated when <see cref="Status"/> is "VALIDATION_FAILED".</summary>
    public IReadOnlyList<ValidationError> Errors { get; init; } = Array.Empty<ValidationError>();

    /// <summary>Human-readable, non-sensitive message describing the outcome.</summary>
    public string? Message { get; init; }

    public static ProxyExecuteResponse Ok(string correlationId, string operationKey, int downstreamStatus, JsonElement? data) => new()
    {
        Success = true,
        CorrelationId = correlationId,
        OperationKey = operationKey,
        Status = "OK",
        DownstreamStatusCode = downstreamStatus,
        Data = data
    };

    public static ProxyExecuteResponse Failure(string correlationId, string operationKey, string status, string message,
        IReadOnlyList<ValidationError>? errors = null, int? downstreamStatusCode = null) => new()
    {
        Success = false,
        CorrelationId = correlationId,
        OperationKey = operationKey,
        Status = status,
        Message = message,
        Errors = errors ?? Array.Empty<ValidationError>(),
        DownstreamStatusCode = downstreamStatusCode
    };
}
