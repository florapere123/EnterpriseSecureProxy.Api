using EnterpriseSecureProxy.Domain.Enums;

namespace EnterpriseSecureProxy.Domain.Entities;

/// <summary>
/// Controls how a single HTTP header is handled when the proxy builds the outbound request.
/// </summary>
public sealed class HeaderRule
{
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
    }
}
