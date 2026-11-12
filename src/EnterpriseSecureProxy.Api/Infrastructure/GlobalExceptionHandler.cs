using Microsoft.AspNetCore.Diagnostics;

namespace EnterpriseSecureProxy.Api.Infrastructure;

/// <summary>Fail-closed fallback when no more specific handler applies.</summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// OWASP Security Misconfiguration: last-resort handler returns a generic 500 without exception details.
    /// </summary>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception.");

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        if (_environment.IsDevelopment())
        {
            var detail = exception.InnerException?.Message ?? exception.Message;
            await httpContext.Response.WriteAsJsonAsync(
                new { message = "An unexpected error occurred.", detail },
                cancellationToken);
        }
        else
        {
            await httpContext.Response.WriteAsJsonAsync(
                new { message = "An unexpected error occurred." },
                cancellationToken);
        }

        return true;
    }
}
