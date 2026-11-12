using EnterpriseSecureProxy.Application.Dtos;
using EnterpriseSecureProxy.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseSecureProxy.Api.Infrastructure;

/// <summary>Maps domain and persistence exceptions to safe HTTP responses (no stack traces or crypto details).</summary>
public sealed class ProxyExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ProxyExceptionHandler> _logger;

    public ProxyExceptionHandler(ILogger<ProxyExceptionHandler> logger) => _logger = logger;

    /// <summary>Map domain failures to safe JSON bodies without stack traces (OWASP Security Misconfiguration).</summary>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        switch (exception)
        {
            case OperationForbiddenException forbidden:
                await WriteProxyExecuteResponseAsync(
                    httpContext,
                    StatusCodes.Status403Forbidden,
                    ResolveCorrelationId(httpContext),
                    forbidden.OperationKey,
                    "FORBIDDEN",
                    forbidden.Message,
                    cancellationToken);
                return true;

            case ProxyDecryptionException decryption:
                await WriteProxyExecuteResponseAsync(
                    httpContext,
                    StatusCodes.Status400BadRequest,
                    ResolveCorrelationId(httpContext),
                    decryption.OperationKey,
                    "DECRYPTION_FAILED",
                    decryption.Message,
                    cancellationToken);
                return true;

            case DbUpdateConcurrencyException:
                _logger.LogWarning("Optimistic concurrency conflict.");
                var conflictOperationKey = httpContext.Request.RouteValues["id"]?.ToString() ?? "admin";
                await WriteProxyExecuteResponseAsync(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    ResolveCorrelationId(httpContext),
                    conflictOperationKey,
                    "CONFIGURATION_CONFLICT",
                    "The resource was modified by another process. Refresh and retry.",
                    cancellationToken);
                return true;

            default:
                return false;
        }
    }

    private static string ResolveCorrelationId(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue("X-Correlation-Id", out var header) &&
            !string.IsNullOrWhiteSpace(header))
        {
            return header.ToString();
        }

        return httpContext.TraceIdentifier;
    }

    private static Task WriteProxyExecuteResponseAsync(
        HttpContext httpContext,
        int statusCode,
        string correlationId,
        string operationKey,
        string status,
        string message,
        CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = statusCode;
        var body = ProxyExecuteResponse.Failure(correlationId, operationKey, status, message);
        return httpContext.Response.WriteAsJsonAsync(body, cancellationToken);
    }
}
