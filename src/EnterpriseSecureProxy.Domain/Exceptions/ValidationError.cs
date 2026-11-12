namespace EnterpriseSecureProxy.Domain.Exceptions;

/// <summary>Represents a single failed parameter or header validation check.</summary>
public sealed class ValidationError
{
    public string Field { get; }

    public string Message { get; }

    /// <summary>Optional machine-readable grouping, e.g. UNKNOWN_PARAMETER or PATH_TRAVERSAL_ATTEMPT.</summary>
    public string? Category { get; }

    public ValidationError(string field, string message)
        : this(field, message, category: null)
    {
    }

    public ValidationError(string field, string message, string? category)
    {
        Field = field;
        Message = message;
        Category = category;
    }
}