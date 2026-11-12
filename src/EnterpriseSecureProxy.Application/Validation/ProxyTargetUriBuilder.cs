using EnterpriseSecureProxy.Application.Dtos;
using EnterpriseSecureProxy.Domain.Entities;
using EnterpriseSecureProxy.Domain.Enums;
using EnterpriseSecureProxy.Domain.Exceptions;

namespace EnterpriseSecureProxy.Application.Validation;

/// <summary>Builds the outbound URI with <c>{param}</c> template substitution and query composition.</summary>
public static class ProxyTargetUriBuilder
{
    /// <summary>Build outbound URI from a validated route map and keep paths under the destination base (OWASP A01).</summary>
    public static (Uri? Uri, IReadOnlyList<ValidationError> Errors) TryBuild(
        Destination destination,
        ProxyOperation operation,
        ProxyExecuteRequest request,
        IReadOnlyDictionary<string, string>? validatedRouteValues = null)
    {
        var errors = new List<ValidationError>();
        IReadOnlyDictionary<string, string> routeValues;
        if (validatedRouteValues is not null)
        {
            routeValues = validatedRouteValues;
        }
        else
        {
            var (resolved, routeErrors) = RouteParameterResolver.Resolve(operation, request);
            if (routeErrors.Count > 0)
            {
                return (null, routeErrors);
            }

            routeValues = resolved;
        }

        var pathTemplate = operation.RelativePath;

        foreach (var rule in operation.ParameterRules.Where(r => r.Location == ParameterLocation.Route))
        {
            if (!routeValues.TryGetValue(rule.Name, out var raw))
            {
                continue;
            }

            var placeholder = "{" + rule.Name + "}";
            if (!pathTemplate.Contains(placeholder, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(new ValidationError(rule.Name,
                    $"Path template does not define placeholder '{placeholder}'."));
                continue;
            }

            var escaped = Uri.EscapeDataString(raw);
            pathTemplate = ReplacePlaceholder(pathTemplate, rule.Name, escaped);
        }

        if (errors.Count > 0)
        {
            return (null, errors);
        }

        if (pathTemplate.Contains('{'))
        {
            errors.Add(new ValidationError("relativePath",
                "One or more required path parameters were not supplied."));
            return (null, errors);
        }

        var relativeForUri = pathTemplate.StartsWith('/') ? pathTemplate : "/" + pathTemplate;
        var targetUri = new Uri(destination.BaseUri, relativeForUri);

        if (!IsUnderDestinationBasePath(destination.BaseUri, targetUri))
        {
            errors.Add(new ValidationError("relativePath",
                "Resolved path escapes the configured destination base path.",
                "PATH_TRAVERSAL_ATTEMPT"));
            return (null, errors);
        }

        if (operation.Method is HttpVerb.Get or HttpVerb.Delete)
        {
            var queryParts = operation.ParameterRules
                .Where(r => r.Location == ParameterLocation.Query)
                .Select(r => r.Name)
                .Select(name =>
                {
                    if (!request.Parameters.TryGetValue(name, out var value) || string.IsNullOrWhiteSpace(value))
                    {
                        return (string?)null;
                    }

                    return $"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value!)}";
                })
                .Where(part => part is not null);

            var queryString = string.Join('&', queryParts);
            if (!string.IsNullOrEmpty(queryString))
            {
                var builder = new UriBuilder(targetUri) { Query = queryString };
                targetUri = builder.Uri;
            }
        }

        return (targetUri, errors);
    }

    private static string ReplacePlaceholder(string template, string paramName, string escapedValue)
    {
        var pattern = "{" + paramName + "}";
        var index = template.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
        while (index >= 0)
        {
            template = template.Remove(index, pattern.Length).Insert(index, escapedValue);
            index = template.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
        }

        return template;
    }

    private static bool IsUnderDestinationBasePath(Uri destinationBase, Uri target)
    {
        var basePath = destinationBase.AbsolutePath.TrimEnd('/');
        if (string.IsNullOrEmpty(basePath) || basePath == "/")
        {
            return true;
        }

        return target.AbsolutePath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase);
    }
}
