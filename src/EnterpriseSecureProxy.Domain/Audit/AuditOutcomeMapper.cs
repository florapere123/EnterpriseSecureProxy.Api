using EnterpriseSecureProxy.Domain.Enums;

namespace EnterpriseSecureProxy.Domain.Audit;

/// <summary>Groups audit outcomes into a coarse category for reporting.</summary>
public static class AuditOutcomeMapper
{
    public static string ToOutcomeCategory(AuditOutcome outcome) => outcome switch
    {
        AuditOutcome.RejectionCryptoFailure => "Rejection_CryptoFailure",
        AuditOutcome.IdempotencyKeyReused => "Rejection_IdempotencyKeyReused",
        AuditOutcome.ServedFromIdempotencyCache => "ServedFromIdempotencyCache",
        _ => outcome.ToString()
    };
}
