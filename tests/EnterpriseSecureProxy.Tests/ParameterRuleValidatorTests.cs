using System.Text.Json;
using EnterpriseSecureProxy.Application.Dtos;
using EnterpriseSecureProxy.Application.Validation;
using EnterpriseSecureProxy.Domain.Entities;
using EnterpriseSecureProxy.Domain.Enums;
using EnterpriseSecureProxy.Domain.Exceptions;
using Xunit;

namespace EnterpriseSecureProxy.Tests;

public sealed class ParameterRuleValidatorTests
{
    [Fact]
    public void UnknownParameter_IsRejected()
    {
        var operation = ProxyOperation.Rehydrate(
            Guid.NewGuid(), "op", "op", "d", HttpVerb.Get, "/x", true, 1024, false,
            ClassificationLevel.Unclassified, Array.Empty<string>(),
            new[] { ParameterRule.Create("a", ParameterLocation.Query, ParameterDataType.String) },
            Array.Empty<HeaderRule>());

        var request = new ProxyExecuteRequest
        {
            ClientId = "op",
            Parameters = new Dictionary<string, string?> { ["a"] = "1", ["extra"] = "x" }
        };

        var errors = new ParameterRuleValidator().Validate(operation, request);
        Assert.Contains(errors, e => e.Category == "UNKNOWN_PARAMETER" && e.Field == "extra");
    }

    [Fact]
    public void UnknownParameter_AllowedWhenFlagSet()
    {
        var operation = ProxyOperation.Rehydrate(
            Guid.NewGuid(), "op", "op", "d", HttpVerb.Get, "/x", true, 1024, false,
            ClassificationLevel.Unclassified, Array.Empty<string>(),
            new[] { ParameterRule.Create("a", ParameterLocation.Query, ParameterDataType.String) },
            Array.Empty<HeaderRule>(),
            allowUnknownParameters: true);

        var request = new ProxyExecuteRequest
        {
            ClientId = "op",
            Parameters = new Dictionary<string, string?> { ["a"] = "1", ["extra"] = "x" }
        };

        var errors = new ParameterRuleValidator().Validate(operation, request);
        Assert.DoesNotContain(errors, e => e.Category == "UNKNOWN_PARAMETER");
    }
}
