namespace EnterpriseSecureProxy.Domain.ValueObjects;

/// <summary>AES-GCM envelope fields (Base64 on the wire).</summary>
public sealed class EncryptedPayload
{
    public string Ciphertext { get; init; } = string.Empty;
    public string Nonce { get; init; } = string.Empty;
    public string Tag { get; init; } = string.Empty;

    public static EncryptedPayload Create(string ciphertext, string nonce, string tag)
    {
        if (string.IsNullOrWhiteSpace(ciphertext))
        {
            throw new ArgumentException("Ciphertext is required.", nameof(ciphertext));
        }

        if (string.IsNullOrWhiteSpace(nonce))
        {
            throw new ArgumentException("Nonce is required.", nameof(nonce));
        }

        if (string.IsNullOrWhiteSpace(tag))
        {
            throw new ArgumentException("Authentication tag is required.", nameof(tag));
        }

        return new EncryptedPayload
        {
            Ciphertext = ciphertext,
            Nonce = nonce,
            Tag = tag
        };
    }
}
