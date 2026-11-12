using EnterpriseSecureProxy.Domain.Enums;

namespace EnterpriseSecureProxy.Domain.Entities;

/// <summary>
/// Controls how a single HTTP header is handled when the proxy builds the outbound request.
/// </summary>
public sealed class HeaderRule
{
    /// <summary>
    /// OWASP Broken Access Control: sensitive hop-by-hop headers cannot be set to forward at config time.
    /// </summary>
    public static readonly IReadOnlySet<string> NeverForwardableHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Cookie",
        "Set-Cookie",
        "Proxy-Authorization",
        "Host",
        "Connection",
        "Transfer-Encoding",
        "Content-Length",
        "Upgrade",
        "TE"
    };

    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public HeaderRuleMode Mode { get; init; } = HeaderRuleMode.Forward;

    public string? InjectedValue { get; init; }

    public bool IsRequired { get; init; }

    public string? Description { get; init; }

    public static HeaderRule Create(
        string name,
        HeaderRuleMode mode,
        string? injectedValue = null,
        bool isRequired = false,
        string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ValidateInjectedValue(mode, injectedValue);
        RejectForwardOfDenylistedHeader(name.Trim(), mode);

        return new HeaderRule
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Mode = mode,
            InjectedValue = injectedValue,
            IsRequired = isRequired,
            Description = description
        };
    }

    public static HeaderRule Rehydrate(
        Guid id,
        string name,
        HeaderRuleMode mode,
        string? injectedValue,
        bool isRequired,
        string? description = null)
    {
        ValidateInjectedValue(mode, injectedValue);

        return new HeaderRule
        {
            Id = id,
            Name = name,
            Mode = mode,
            InjectedValue = injectedValue,
            IsRequired = isRequired,
            Description = description
        };
    }

    private static void ValidateInjectedValue(HeaderRuleMode mode, string? injectedValue)
    {
        if (mode == HeaderRuleMode.Inject && string.IsNullOrWhiteSpace(injectedValue))
        {
            throw new ArgumentException("InjectedValue is required when mode is Inject.", nameof(injectedValue));
        }

        // OWASP Injection: a CR/LF in an injected value could smuggle extra headers into the outbound request.
        if (injectedValue is not null && (injectedValue.Contains('\r') || injectedValue.Contains('\n')))
        {
            throw new InvalidOperationException(
                "InjectedValue must not contain CR or LF characters; header injection is not permitted.");
        }
    }

    private static void RejectForwardOfDenylistedHeader(string name, HeaderRuleMode mode)
    {
        if (mode == HeaderRuleMode.Forward && NeverForwardableHeaders.Contains(name))
        {
            throw new InvalidOperationException(
                $"Header '{name}' cannot be configured with mode Forward; it is on the non-configurable denylist.");
        }
    }

    public static bool IsNeverForwardable(string headerName) =>
        NeverForwardableHeaders.Contains(headerName);
}
