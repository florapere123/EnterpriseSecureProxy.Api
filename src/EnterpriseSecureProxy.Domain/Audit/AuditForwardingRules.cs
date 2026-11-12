using EnterpriseSecureProxy.Domain.Enums;

namespace EnterpriseSecureProxy.Domain.Audit;

public static class AuditForwardingRules
{
    /// <summary>
    /// OWASP Security Logging: Forwarded must match a real downstream attempt or incident analysis will be wrong.
    /// </summary>
    public static bool WasForwardedToDestination(AuditOutcome outcome) =>
        outcome is AuditOutcome.Succeeded or AuditOutcome.DestinationError;
}
