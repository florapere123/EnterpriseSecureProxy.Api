using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using EnterpriseSecureProxy.Application.Dtos;
using EnterpriseSecureProxy.Application.Interfaces;
using EnterpriseSecureProxy.Domain.Entities;
using EnterpriseSecureProxy.Domain.Enums;
using EnterpriseSecureProxy.Domain.Exceptions;

namespace EnterpriseSecureProxy.Application.Validation;

/// <summary>
/// Default <see cref="IRequestValidator"/>. Evaluates every <see cref="ParameterRule"/> on the
/// operation against the inbound request: presence, data type, allowlist, pattern and range.
/// Body-located rules are resolved against top-level JSON properties of the request body.
/// </summary>
public sealed class ParameterRuleValidator : IRequestValidator
{
    /// <summary>Reject unknown fields and validate configured parameters only (OWASP Injection).</summary>
    public IReadOnlyList<ValidationError> Validate(ProxyOperation operation, ProxyExecuteRequest request)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(request);

        var errors = new List<ValidationError>();
        var (routeValues, routeErrors) = RouteParameterResolver.Resolve(operation, request);
        errors.AddRange(routeErrors);

        foreach (var rule in operation.ParameterRules)
        {
            if (rule.Location == ParameterLocation.Route)
            {
                if (routeValues.TryGetValue(rule.Name, out var routeRaw))
                {
                    ValidateValue(rule, routeRaw, errors);
                }

                continue;
            }

            var (found, rawValue) = ResolveValue(rule, request);

            if (!found || string.IsNullOrWhiteSpace(rawValue))
            {
                if (rule.IsRequired)
                {
                    errors.Add(new ValidationError(rule.Name, $"Parameter '{rule.Name}' is required."));
                }

                continue;
            }

            ValidateValue(rule, rawValue!, errors);
        }

        if (!operation.AllowUnknownParameters)
        {
            RejectUnknownParameters(operation, request, errors);
        }

        return errors;
    }

    private static void RejectUnknownParameters(
        ProxyOperation operation,
        ProxyExecuteRequest request,
        List<ValidationError> errors)
    {
        var known = operation.ParameterRules
            .Select(r => r.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var key in request.Parameters.Keys)
        {
            if (!known.Contains(key))
            {
                errors.Add(new ValidationError(key, $"Unknown parameter '{key}'.", "UNKNOWN_PARAMETER"));
            }
        }

        if (request.Body is { ValueKind: JsonValueKind.Object } body)
        {
            foreach (var prop in body.EnumerateObject())
            {
                if (!known.Contains(prop.Name))
                {
                    errors.Add(new ValidationError(prop.Name,
                        $"Unknown parameter '{prop.Name}'.",
                        "UNKNOWN_PARAMETER"));
                }
            }
        }
    }

    private static (bool Found, string? Value) ResolveValue(ParameterRule rule, ProxyExecuteRequest request)
    {
        if (rule.Location == ParameterLocation.Body && request.Body is { } body)
        {
            if (body.ValueKind == JsonValueKind.Object &&
                body.TryGetProperty(rule.Name, out var prop))
            {
                return (true, prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.GetRawText());
            }

            return (false, null);
        }

        if (rule.Location is ParameterLocation.Query or ParameterLocation.Route or ParameterLocation.Header)
        {
            if (request.Parameters.TryGetValue(rule.Name, out var value))
            {
                return (true, value);
            }
        }

        return (false, null);
    }

    private static void ValidateValue(ParameterRule rule, string value, List<ValidationError> errors)
    {
        if (!TryCheckDataType(rule.DataType, value, out var numericValue))
        {
            errors.Add(new ValidationError(rule.Name, $"Parameter '{rule.Name}' must be a valid {rule.DataType}."));
            return;
        }

        if (rule.AllowedValues is { Count: > 0 } &&
            !rule.AllowedValues.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add(new ValidationError(rule.Name,
                $"Parameter '{rule.Name}' must be one of: {string.Join(", ", rule.AllowedValues)}."));
        }

        if (!string.IsNullOrEmpty(rule.Pattern) &&
            !Regex.IsMatch(value, rule.Pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(250)))
        {
            errors.Add(new ValidationError(rule.Name, $"Parameter '{rule.Name}' does not match the required format."));
        }

        var comparable = numericValue ?? value.Length;
        if (rule.MinValue is { } min && comparable < min)
        {
            errors.Add(new ValidationError(rule.Name, $"Parameter '{rule.Name}' must be >= {min}."));
        }

        if (rule.MaxValue is { } max && comparable > max)
        {
            errors.Add(new ValidationError(rule.Name, $"Parameter '{rule.Name}' must be <= {max}."));
        }
    }

    private static bool TryCheckDataType(ParameterDataType dataType, string value, out double? numericValue)
    {
        numericValue = null;
        switch (dataType)
        {
            case ParameterDataType.String:
                return true;
            case ParameterDataType.Integer:
                if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
                {
                    numericValue = l;
                    return true;
                }
                return false;
            case ParameterDataType.Decimal:
                if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                {
                    numericValue = d;
                    return true;
                }
                return false;
            case ParameterDataType.Boolean:
                return bool.TryParse(value, out _);
            case ParameterDataType.DateTimeUtc:
                return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out _);
            case ParameterDataType.Guid:
                return Guid.TryParse(value, out _);
            case ParameterDataType.Json:
                try { using var _ = JsonDocument.Parse(value); return true; }
                catch (JsonException) { return false; }
            default:
                return true;
        }
    }
}
