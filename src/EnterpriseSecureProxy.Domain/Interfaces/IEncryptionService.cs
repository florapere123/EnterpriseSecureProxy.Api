using EnterpriseSecureProxy.Domain.ValueObjects;

namespace EnterpriseSecureProxy.Domain.Interfaces;

/// <summary>Authenticated encryption for operation-gated proxy payloads.</summary>
public interface IEncryptionService
{
    byte[] Decrypt(EncryptedPayload payload, ReadOnlySpan<byte> associatedData);

    EncryptedPayload Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> associatedData);
}
