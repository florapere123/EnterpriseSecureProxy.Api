using EnterpriseSecureProxy.Domain.Entities;
using EnterpriseSecureProxy.Domain.Enums;
using EnterpriseSecureProxy.Infrastructure.Persistence.Entities;

namespace EnterpriseSecureProxy.Infrastructure.Persistence;

/// <summary>Maps EF persistence entities to and from the domain configuration types.</summary>
internal static class ConfigurationEntityMapper
{
    public static Destination ToDomain(DestinationEntity entity) =>
        Destination.Rehydrate(
            entity.Id,
            entity.Name,
            new Uri(entity.BaseUrl, UriKind.Absolute),
            TimeSpan.FromMilliseconds(entity.TimeoutMilliseconds),
            entity.IsActive,
            settingsJson: entity.SettingsJson);

    public static ProxyOperation ToDomain(ProxyOperationEntity entity)
    {
        var destinationName = entity.Destination?.Name ?? string.Empty;

        return ProxyOperation.Rehydrate(
            entity.Id,
            entity.OperationKey,
            entity.OperationKey,
            destinationName,
            ParseHttpVerb(entity.HttpMethod),
            entity.RelativePath,
            entity.IsActive,
            entity.MaxRequestSize,
            entity.RequiresEncryption,
            ClassificationLevel.Unclassified,
            ParseRoles(entity.AllowedRoles),
            entity.ParameterRules.Select(ToDomain).ToList(),
            entity.HeaderRules.Select(ToDomain).Where(h => h is not null).Cast<HeaderRule>().ToList(),
            description: null,
            allowUnknownParameters: entity.AllowUnknownParameters,
            allowHttpClientRetryForDelete: entity.AllowHttpClientRetryForDelete,
            timeout: entity.TimeoutMilliseconds is { } ms ? TimeSpan.FromMilliseconds(ms) : null);
    }

    private static ParameterRule ToDomain(ParameterRuleEntity entity) =>
        ParameterRule.Rehydrate(
            entity.Id,
            entity.ParameterName,
            ParseLocation(entity.Location),
            ParseDataType(entity.DataType),
            entity.IsRequired,
            ParseAllowedValues(entity.AllowedValues),
            entity.Pattern,
            minValue: null,
            maxValue: entity.MaxLength);

    private static HeaderRule? ToDomain(HeaderRuleEntity entity)
    {
        var mode = ParseHeaderMode(entity.Mode);
        if (mode == HeaderRuleMode.Forward && HeaderRule.IsNeverForwardable(entity.HeaderName))
        {
            return null;
        }

        return HeaderRule.Rehydrate(
            entity.Id,
            entity.HeaderName,
            mode,
            entity.InjectedValue,
            entity.IsRequired);
    }

    private static HttpVerb ParseHttpVerb(string value) =>
        Enum.TryParse<HttpVerb>(value.Trim(), ignoreCase: true, out var verb)
            ? verb
            : value.Trim().ToUpperInvariant() switch
            {
                "GET" => HttpVerb.Get,
                "POST" => HttpVerb.Post,
                "PUT" => HttpVerb.Put,
                "PATCH" => HttpVerb.Patch,
                "DELETE" => HttpVerb.Delete,
                _ => HttpVerb.Post
            };

    private static ParameterLocation ParseLocation(string value) =>
        value.Trim().ToUpperInvariant() switch
        {
            "PATH" or "ROUTE" => ParameterLocation.Route,
            "QUERY" => ParameterLocation.Query,
            "HEADER" => ParameterLocation.Header,
            _ => ParameterLocation.Body
        };

    private static ParameterDataType ParseDataType(string value) =>
        Enum.TryParse<ParameterDataType>(value.Trim(), ignoreCase: true, out var type)
            ? type
            : ParameterDataType.String;

    private static HeaderRuleMode ParseHeaderMode(string value) =>
        value.Trim().ToUpperInvariant() switch
        {
            "INJECT" => HeaderRuleMode.Inject,
            "BLOCK" => HeaderRuleMode.Block,
            _ => HeaderRuleMode.Forward
        };

    private static IReadOnlyList<string> ParseRoles(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<string>();
        }

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
    }

    private static IReadOnlyList<string>? ParseAllowedValues(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
    }
}
