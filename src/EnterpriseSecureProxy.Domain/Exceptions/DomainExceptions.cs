namespace EnterpriseSecureProxy.Domain.Exceptions;

/// <summary>Base type for all domain-level failures raised by the secure proxy.</summary>
public abstract class ProxyDomainException : Exception
{
    protected ProxyDomainException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}

/// <summary>Raised when an inbound request references an operation key that is not configured or is disabled.</summary>
public sealed class OperationNotFoundException : ProxyDomainException
{
    public string OperationKey { get; }

    public OperationNotFoundException(string operationKey)
        : base($"No enabled proxy operation is configured for key '{operationKey}'.")
    {
        OperationKey = operationKey;
    }
}

/// <summary>Raised when a configured operation points at a destination that cannot be resolved.</summary>
public sealed class DestinationNotFoundException : ProxyDomainException
{
    public string DestinationName { get; }

    public DestinationNotFoundException(string destinationName)
        : base($"No enabled destination is configured with name '{destinationName}'.")
    {
        DestinationName = destinationName;
    }
}

/// <summary>Raised when an inbound request fails one or more parameter/header validation rules.</summary>
public sealed class ProxyValidationException : ProxyDomainException
{
    public IReadOnlyList<ValidationError> Errors { get; }

    public ProxyValidationException(IReadOnlyList<ValidationError> errors)
        : base($"Request validation failed with {errors.Count} error(s).")
    {
        Errors = errors;
    }
}

/// <summary>Raised when the principal is authenticated but not authorized for the requested operation.</summary>
public sealed class OperationForbiddenException : ProxyDomainException
{
    public string OperationKey { get; }

    public OperationForbiddenException(string operationKey, string reason)
        : base($"Access to operation '{operationKey}' was denied: {reason}")
    {
        OperationKey = operationKey;
    }
}

/// <summary>Raised when an encrypted request cannot be decrypted or fails authentication.</summary>
public sealed class ProxyDecryptionException : ProxyDomainException
{
    public string OperationKey { get; }

    public ProxyDecryptionException(string operationKey)
        : base("Invalid or corrupted encrypted payload.")
    {
        OperationKey = operationKey;
    }
}
