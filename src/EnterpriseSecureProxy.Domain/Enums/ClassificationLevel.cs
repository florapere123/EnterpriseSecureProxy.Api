namespace EnterpriseSecureProxy.Domain.Enums;

/// <summary>Security classification ceiling/floor used for RBAC and data-handling decisions.</summary>
public enum ClassificationLevel
{
    Unclassified = 0,
    Restricted = 1,
    Confidential = 2,
    Secret = 3,
    TopSecret = 4
}
