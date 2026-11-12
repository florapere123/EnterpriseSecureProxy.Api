namespace EnterpriseSecureProxy.Infrastructure.Security;

public sealed class SecuritySettings
{
    public const string SectionName = "Security";

    /// <summary>
    /// 32-byte AES-256 key, 
    /// Base64-encoded (e.g. from Key Vault).
    /// </summary>
    public string MasterKeyBase64 { get; set; } = string.Empty;
}
