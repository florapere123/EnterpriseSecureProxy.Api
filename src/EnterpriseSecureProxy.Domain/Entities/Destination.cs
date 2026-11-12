namespace EnterpriseSecureProxy.Domain.Entities;

/// <summary>
/// An internal microservice the proxy is allowed to forward traffic to.
/// Base addresses are never supplied by callers; they are resolved exclusively from configuration,
/// which prevents the proxy from being abused as an open (SSRF) relay.
/// </summary>
public sealed class Destination
{
    public Guid Id { get; init; }

    /// <summary>Stable logical name, e.g. "c4i-targeting-service".</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Absolute base URI of the internal service, e.g. "https://targeting.internal:8443".</summary>
    public Uri BaseUri { get; init; } = null!;

    /// <summary>Per-destination timeout for the outbound call.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>When false the destination is administratively disabled and all operations fail fast.</summary>
    public bool IsEnabled { get; init; } = true;

    public string? Description { get; init; }

    public static Destination Create(
        string name,
        Uri baseUri,
        TimeSpan? timeout = null,
        bool isEnabled = true,
        string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ValidateBaseUri(baseUri);

        return new Destination
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            BaseUri = baseUri,
            Timeout = timeout ?? TimeSpan.FromSeconds(30),
            IsEnabled = isEnabled,
            Description = description
        };
    }

    /// <summary>Rehydrates a destination from persistence without generating a new identity.</summary>
    public static Destination Rehydrate(
        Guid id,
        string name,
        Uri baseUri,
        TimeSpan timeout,
        bool isEnabled,
        string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ValidateBaseUri(baseUri);

        return new Destination
        {
            Id = id,
            Name = name,
            BaseUri = baseUri,
            Timeout = timeout,
            IsEnabled = isEnabled,
            Description = description
        };
    }

    private static void ValidateBaseUri(Uri baseUri)
    {
        ArgumentNullException.ThrowIfNull(baseUri);
        if (!baseUri.IsAbsoluteUri)
        {
            throw new ArgumentException("Base URI must be absolute.", nameof(baseUri));
        }
    }
}
