namespace EnterpriseSecureProxy.Domain.Enums;

/// <summary>Where a parameter is expected to be found on the inbound request.</summary>
public enum ParameterLocation
{
    Query = 1,
    Route = 2,
    Body = 3,
    Header = 4
}
