using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EnterpriseSecureProxy.Api.Infrastructure;

/// <summary>Adds proxy header fields to Swagger for POST /api/proxy/execute only.</summary>
public sealed class ProxyHeadersOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.ActionDescriptor is not ControllerActionDescriptor action)
        {
            return;
        }

        if (!string.Equals(action.ControllerName, "Proxy", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(action.ActionName, "Execute", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        operation.Parameters ??= new List<OpenApiParameter>();

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Client-Role",
            In = ParameterLocation.Header,
            Required = false,
            Schema = new OpenApiSchema { Type = "string" },
            Description =
                "Development only: role for local RBAC testing (e.g. operator, admin, user). Ignored outside Development."
        });

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "Idempotency-Key",
            In = ParameterLocation.Header,
            Required = false,
            Schema = new OpenApiSchema { Type = "string" },
            Description = "Required when the operation uses POST — replays return the cached downstream response."
        });

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Correlation-Id",
            In = ParameterLocation.Header,
            Required = false,
            Schema = new OpenApiSchema { Type = "string" },
            Description = "Optional tracing id echoed in the response and audit logs."
        });
    }
}
