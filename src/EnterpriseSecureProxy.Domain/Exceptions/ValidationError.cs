namespace EnterpriseSecureProxy.Domain.Exceptions;

/// <summary>Represents a single failed parameter or header validation check.</summary>
public sealed record ValidationError(string Field, string Message);
