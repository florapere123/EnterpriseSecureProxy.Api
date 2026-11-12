namespace EnterpriseSecureProxy.Application.UseCases;

/// <summary>
/// Ambient, transport-sourced information about a single invocation that the use case needs but
/// that does not belong in the request DTO (principal, correlation id, inbound headers, caller ip).
/// The API layer constructs this from the <c>HttpContext</c>.
/// </summary>
public sealed class ProxyInvocationContext
{
    public required string CorrelationId { get; init; }

    public string PrincipalId { get; init; } = "anonymous";

    /// <summary>Roles granted to the authenticated principal, used for RBAC decisions.</summary>
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();

    /// <summary>Caller role supplied via the <c>X-Client-Role</c> header (C4I client contract).</summary>
    public string? ClientRole { get; init; }

    public string? SourceAddress { get; init; }

    /// <summary>Inbound <c>Content-Length</c> when present (used for <see cref="Domain.Entities.ProxyOperation.MaxRequestSize"/> checks).</summary>
    public long? RequestContentLengthBytes { get; init; }

    /// <summary>Duplicate-suppression key for POST proxy operations (<c>Idempotency-Key</c> header).</summary>
    public string? IdempotencyKey { get; init; }

    /// <summary>Inbound request headers, consulted when resolving Forward header rules.</summary>
    public IReadOnlyDictionary<string, string> InboundHeaders { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
