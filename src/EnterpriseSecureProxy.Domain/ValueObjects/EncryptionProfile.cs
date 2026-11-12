namespace EnterpriseSecureProxy.Domain.ValueObjects;

/// <summary>Declares whether and how inbound payloads must be encrypted before forwarding.</summary>
public sealed class EncryptionProfile
{
    public bool IsRequired { get; init; }

    public string Algorithm { get; init; } = "AES-GCM-256";

    public static EncryptionProfile None { get; } = new() { IsRequired = false };

    public static EncryptionProfile AesGcm256Required { get; } = new() { IsRequired = true, Algorithm = "AES-GCM-256" };

    public static EncryptionProfile FromRequiresEncryption(bool requiresEncryption) =>
        requiresEncryption ? AesGcm256Required : None;
}
