namespace EnterpriseSecureProxy.Domain.Enums;

/// <summary>Terminal outcome of a proxied operation, written to the audit trail.</summary>
public enum AuditOutcome
{
    Succeeded = 1,
    ValidationFailed = 2,
    Forbidden = 3,
    OperationNotFound = 4,
    DestinationUnavailable = 5,
    DestinationError = 6,
    RateLimited = 7,
    PayloadTooLarge = 8,
    EncryptionInvalid = 9,
    RejectionCryptoFailure = 10,
    ServedFromIdempotencyCache = 11
}
