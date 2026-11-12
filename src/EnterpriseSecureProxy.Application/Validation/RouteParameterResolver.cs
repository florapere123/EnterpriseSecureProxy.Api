using EnterpriseSecureProxy.Application.Dtos;
using EnterpriseSecureProxy.Domain.Entities;
using EnterpriseSecureProxy.Domain.Enums;
using EnterpriseSecureProxy.Domain.Exceptions;

namespace EnterpriseSecureProxy.Application.Validation;

public static class RouteParameterResolver
{
    /// <summary>Checks required route params and blocks traversal payloads before substitution (OWASP A01).</summary>
    public static (IReadOnlyDictionary<string, string> Values, IReadOnlyList<ValidationError> Errors) Resolve(
        ProxyOperation operation,
        ProxyExecuteRequest request)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var errors = new List<ValidationError>();

        foreach (var rule in operation.ParameterRules.Where(r => r.Location == ParameterLocation.Route))
        {
            if (!request.Parameters.TryGetValue(rule.Name, out var raw) || string.IsNullOrWhiteSpace(raw))
            {
                if (rule.IsRequired)
                {
                    errors.Add(new ValidationError(rule.Name, $"Parameter '{rule.Name}' is required."));
                }

                continue;
            }

            if (PathParameterSecurity.ContainsTraversalAttempt(raw))
            {
                errors.Add(new ValidationError(rule.Name,
                    $"Parameter '{rule.Name}' contains a path traversal attempt.",
                    "PATH_TRAVERSAL_ATTEMPT"));
                continue;
            }

            values[rule.Name] = raw;
        }

        return (values, errors);
    }
}
