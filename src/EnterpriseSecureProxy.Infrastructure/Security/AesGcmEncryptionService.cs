using System.Security.Cryptography;
using System.Text;
using EnterpriseSecureProxy.Domain.Interfaces;
using EnterpriseSecureProxy.Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace EnterpriseSecureProxy.Infrastructure.Security;

/// <summary>Encrypts and decrypts proxy payloads with AES-256-GCM using the configured master key.</summary>
public sealed class AesGcmEncryptionService : IEncryptionService
{
    private const int NonceSize = 12; // 96-bit nonce — NIST-recommended size for AES-GCM
    private const int TagSize = 16;   // 128-bit auth tag — detects ciphertext tampering on decrypt
    private readonly byte[] _key;

    public AesGcmEncryptionService(IOptions<SecuritySettings> options)
    {
        var keyB64 = options.Value.MasterKeyBase64;
        if (string.IsNullOrWhiteSpace(keyB64))
        {
            throw new InvalidOperationException("Security:MasterKeyBase64 is not configured.");
        }

        _key = Convert.FromBase64String(keyB64);
        if (_key.Length != 32) // AES-256 requires a 32-byte (256-bit) key
        {
            throw new InvalidOperationException("Security:MasterKeyBase64 must decode to exactly 32 bytes.");
        }
    }

    /// <summary>
    /// OWASP Cryptographic Failures: AES-GCM checks the auth tag; tampered payloads fail without leaking keys or plaintext in errors.
    /// </summary>
    public byte[] Decrypt(EncryptedPayload payload, ReadOnlySpan<byte> associatedData)
    {
        var ciphertext = Convert.FromBase64String(payload.Ciphertext);
        var nonce = Convert.FromBase64String(payload.Nonce);
        var tag = Convert.FromBase64String(payload.Tag);

        if (nonce.Length != NonceSize)
        {
            throw new CryptographicException("Invalid nonce size.");
        }

        if (tag.Length != TagSize)
        {
            throw new CryptographicException("Invalid tag size.");
        }

        var plaintext = new byte[ciphertext.Length];
        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);
        return plaintext;
    }

    public EncryptedPayload Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> associatedData)
    {
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);

        return EncryptedPayload.Create(
            Convert.ToBase64String(ciphertext),
            Convert.ToBase64String(nonce),
            Convert.ToBase64String(tag));
    }

    public static ReadOnlySpan<byte> ToAssociatedData(string operationKey) =>
        Encoding.UTF8.GetBytes(operationKey);
}
