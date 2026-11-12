using EnterpriseSecureProxy.Application.Dtos;
using EnterpriseSecureProxy.Application.UseCases;
using EnterpriseSecureProxy.Api.Infrastructure;
using EnterpriseSecureProxy.Infrastructure.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EnterpriseSecureProxy.Api.Controllers;

/// <summary>Public entry point for proxy calls; builds the invocation context and hands off to the handler.</summary>
[ApiController]
[Route("api/proxy")]
[Produces("application/json")]
public sealed class ProxyController : ControllerBase
{
    private readonly ExecuteProxyOperationHandler _handler;
    private readonly GatewayTrustOptions _gatewayTrust;
    private readonly IWebHostEnvironment _environment;

    public ProxyController(
        ExecuteProxyOperationHandler handler,
        IOptions<GatewayTrustOptions> gatewayTrust,
        IWebHostEnvironment environment)
    {
        _handler = handler;
        _gatewayTrust = gatewayTrust.Value;
        _environment = environment;
    }

    /// <summary>Run a configured proxy operation using clientId and optional body/parameters.</summary>
    [HttpPost("execute")]
    [ApiConventionMethod(typeof(ProxyApiConventions), nameof(ProxyApiConventions.Execute))]
    public async Task<IActionResult> Execute([FromBody] ProxyExecuteRequest request, CancellationToken cancellationToken)
    {
        var context = BuildContext();
        var result = await _handler.HandleAsync(request, context, cancellationToken);
        return StatusCode(ProxyStatusCodeMapper.ToHttpStatusCode(result.Status), result);
    }

    /// <summary>
    /// OWASP Broken Access Control: trust the gateway role header only when configured; X-Client-Role works in Development only.
    /// </summary>
    private ProxyInvocationContext BuildContext()
    {
        var correlationId = Request.Headers.TryGetValue("X-Correlation-Id", out var header) && !string.IsNullOrWhiteSpace(header)
            ? header.ToString()
            : HttpContext.TraceIdentifier;

        var inboundHeaders = Request.Headers.ToDictionary(
            h => h.Key,
            h => h.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);

        var principal = User.Identity?.IsAuthenticated == true
            ? User.Identity!.Name ?? "authenticated"
            : "anonymous";

        var roles = User.Claims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .ToArray();

        // Role is derived only from a trusted gateway-asserted header. The self-asserted X-Client-Role
        // is accepted strictly as a local-development convenience, never in any other environment.
        string? clientRole = null;
        if (_gatewayTrust.TrustGatewayHeaders &&
            Request.Headers.TryGetValue("X-Gateway-Asserted-Role", out var gatewayRole) &&
            !string.IsNullOrWhiteSpace(gatewayRole))
        {
            clientRole = gatewayRole.ToString();
        }
        else if (_environment.IsDevelopment() &&
            Request.Headers.TryGetValue("X-Client-Role", out var devRole) &&
            !string.IsNullOrWhiteSpace(devRole))
        {
            clientRole = devRole.ToString();
        }

        var idempotencyKey = Request.Headers.TryGetValue("Idempotency-Key", out var idem) && !string.IsNullOrWhiteSpace(idem)
            ? idem.ToString()
            : null;

        return new ProxyInvocationContext
        {
            CorrelationId = correlationId,
            PrincipalId = principal,
            Roles = roles,
            ClientRole = clientRole,
            SourceAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            RequestContentLengthBytes = Request.ContentLength,
            IdempotencyKey = idempotencyKey,
            InboundHeaders = inboundHeaders
        };
    }
}
