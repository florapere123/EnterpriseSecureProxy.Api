using EnterpriseSecureProxy.Domain.Enums;
using EnterpriseSecureProxy.Domain.ValueObjects;

namespace EnterpriseSecureProxy.Domain.Entities;

/// <summary>
/// The central configuration aggregate. Maps a caller-facing operation key to a destination,
/// verb, relative path, and the validation and header policies enforced before forwarding.
/// </summary>
public sealed class ProxyOperation
{
    public const int DefaultMaxRequestSizeBytes = 1_048_576;

    public Guid Id { get; init; }

    /// <summary>
    /// Unique operation key, e.g. "target.report.location" (inbound <c>clientId</c>).
    /// </summary>
    public string OperationKey { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Logical name of the <see cref="Destination"/> this operation forwards to.</summary>
    public string DestinationName { get; init; } = string.Empty;

    public HttpVerb Method { get; init; } = HttpVerb.Post;

    /// <summary>Relative path appended to the destination base URI, e.g. "/api/v1/sensors/reading".</summary>
    public string RelativePath { get; init; } = string.Empty;

    public ClassificationLevel RequiredClassification { get; init; } = ClassificationLevel.Unclassified;

    public IReadOnlyList<string> AllowedRoles { get; init; } = Array.Empty<string>();

    public IReadOnlyList<ParameterRule> ParameterRules { get; init; } = Array.Empty<ParameterRule>();

    public IReadOnlyList<HeaderRule> HeaderRules { get; init; } = Array.Empty<HeaderRule>();

    public bool IsEnabled { get; init; } = true;

    public int MaxRequestSize { get; init; } = DefaultMaxRequestSizeBytes;

    /// <summary>Optional per-operation outbound timeout; falls back to destination timeout when unset.</summary>
    public TimeSpan? Timeout { get; init; }

    public bool RequiresEncryption { get; init; }

    /// <summary>When true, inbound parameters not covered by a <see cref="ParameterRule"/> are allowed.</summary>
    public bool AllowUnknownParameters { get; init; }

    /// <summary>When true, DELETE operations may be retried at the HTTP client resilience layer.</summary>
    public bool AllowHttpClientRetryForDelete { get; init; }

    public string? Description { get; init; }

    public EncryptionProfile EncryptionProfile => EncryptionProfile.FromRequiresEncryption(RequiresEncryption);

    public static ProxyOperation Create(
        string operationKey,
        string displayName,
        string destinationName,
        HttpVerb method,
        string relativePath,
        bool requiresEncryption = false,
        IEnumerable<string>? allowedRoles = null,
        IEnumerable<ParameterRule>? parameterRules = null,
        IEnumerable<HeaderRule>? headerRules = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationName);
        ValidateRelativePath(relativePath);

        return new ProxyOperation
        {
            Id = Guid.NewGuid(),
            OperationKey = operationKey.Trim(),
            DisplayName = displayName.Trim(),
            DestinationName = destinationName.Trim(),
            Method = method,
            RelativePath = NormalizeRelativePath(relativePath),
            RequiresEncryption = requiresEncryption,
            AllowedRoles = MaterializeReadOnlyList(allowedRoles),
            ParameterRules = MaterializeReadOnlyList(parameterRules),
            HeaderRules = MaterializeReadOnlyList(headerRules)
        };
    }

    public static ProxyOperation Rehydrate(
        Guid id,
        string operationKey,
        string displayName,
        string destinationName,
        HttpVerb method,
        string relativePath,
        bool isEnabled,
        int maxRequestSize,
        bool requiresEncryption,
        ClassificationLevel requiredClassification,
        IReadOnlyList<string> allowedRoles,
        IReadOnlyList<ParameterRule> parameterRules,
        IReadOnlyList<HeaderRule> headerRules,
        string? description = null,
        bool allowUnknownParameters = false,
        bool allowHttpClientRetryForDelete = false,
        TimeSpan? timeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationName);

        return new ProxyOperation
        {
            Id = id,
            OperationKey = operationKey,
            DisplayName = displayName,
            DestinationName = destinationName,
            Method = method,
            RelativePath = relativePath,
            IsEnabled = isEnabled,
            MaxRequestSize = maxRequestSize,
            RequiresEncryption = requiresEncryption,
            RequiredClassification = requiredClassification,
            AllowedRoles = allowedRoles,
            ParameterRules = parameterRules,
            HeaderRules = headerRules,
            Description = description,
            AllowUnknownParameters = allowUnknownParameters,
            AllowHttpClientRetryForDelete = allowHttpClientRetryForDelete,
            Timeout = timeout
        };
    }

    private static string NormalizeRelativePath(string relativePath)
    {
        ValidateRelativePath(relativePath);
        var trimmed = relativePath.Trim();
        return trimmed.StartsWith("/", StringComparison.Ordinal) ? trimmed : $"/{trimmed}";
    }

    private static void ValidateRelativePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("Relative path is required.", nameof(relativePath));
        }
    }

    private static IReadOnlyList<T> MaterializeReadOnlyList<T>(IEnumerable<T>? items) =>
        items?.ToArray() ?? Array.Empty<T>();
}
