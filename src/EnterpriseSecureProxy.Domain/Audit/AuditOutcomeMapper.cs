using EnterpriseSecureProxy.Domain.Enums;

namespace EnterpriseSecureProxy.Domain.Audit;

public static class AuditOutcomeMapper
{
    public static string ToOutcomeCategory(AuditOutcome outcome) => outcome switch
    {
        AuditOutcome.RejectionCryptoFailure => "Rejection_CryptoFailure",
        AuditOutcome.ServedFromIdempotencyCache => "ServedFromIdempotencyCache",
        _ => outcome.ToString()
    };
}
