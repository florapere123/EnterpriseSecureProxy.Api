using System.Text.Json;
using EnterpriseSecureProxy.Domain.ValueObjects;

namespace EnterpriseSecureProxy.Application.Dtos;

/// <summary>
/// Inbound payload for <c>POST /api/proxy/execute</c>. The caller identifies the operation it
/// wishes to invoke via <see cref="ClientId"/> and supplies the parameters/body the operation needs.
/// </summary>
public sealed class ProxyExecuteRequest
{
    /// <summary>
    /// Operation key (e.g. "target.report.location"). Named <c>clientId</c> on the wire to match
    /// the existing C4I client contract; it is resolved to a configured operation by the Application layer.
    /// </summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>Flat parameter bag used for query/route/simple body parameters.</summary>
    public IDictionary<string, string?> Parameters { get; init; } = new Dictionary<string, string?>(StringComparer.Ordinal);

    /// <summary>Optional raw JSON body forwarded to body-oriented operations (e.g. classification updates).</summary>
    public JsonElement? Body { get; init; }

    /// <summary>AES-GCM ciphertext (Base64) when the operation requires encryption.</summary>
    public string? Ciphertext { get; init; }

    /// <summary>12-byte AES-GCM nonce/IV (Base64).</summary>
    public string? Nonce { get; init; }

    /// <summary>16-byte AES-GCM authentication tag (Base64).</summary>
    public string? Tag { get; init; }

    public EncryptedPayload? ToEncryptedPayload()
    {
        if (string.IsNullOrWhiteSpace(Ciphertext) ||
            string.IsNullOrWhiteSpace(Nonce) ||
            string.IsNullOrWhiteSpace(Tag))
        {
            return null;
        }

        return EncryptedPayload.Create(Ciphertext, Nonce, Tag);
    }
}
