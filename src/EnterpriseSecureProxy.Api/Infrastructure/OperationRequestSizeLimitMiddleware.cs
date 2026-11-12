using System.Text.Json;
using EnterpriseSecureProxy.Domain.Interfaces;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;

namespace EnterpriseSecureProxy.Api.Infrastructure;

/// <summary>
/// Enforces a global request-size floor, then applies the per-operation
/// <see cref="EnterpriseSecureProxy.Domain.Entities.ProxyOperation.MaxRequestSize"/> to Kestrel's
/// body-size feature. Operation lookup peeks only a bounded prefix of the body so it can never be
/// used to force unbounded buffering.
/// </summary>
public sealed class OperationRequestSizeLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RequestLimitsOptions _limits;

    public OperationRequestSizeLimitMiddleware(RequestDelegate next, IOptions<RequestLimitsOptions> limits)
    {
        _next = next;
        _limits = limits.Value;
    }

    /// <summary>
    /// OWASP Insecure Design: enforce a global size cap first and only peek at the first few KB for clientId so huge bodies cannot exhaust memory.
    /// </summary>
    public async Task InvokeAsync(HttpContext context, IConfigurationRepository configuration)
    {
        if (HttpMethods.IsPost(context.Request.Method))
        {
            // Global floor first: reject obviously-oversized requests before any operation-specific logic.
            if (context.Request.ContentLength is { } declaredLength && declaredLength > _limits.MaxGlobalBodyBytes)
            {
                await RejectTooLargeAsync(context);
                return;
            }

            context.Request.EnableBuffering();
            var operationKey = await TryPeekClientIdAsync(context.Request, _limits.OperationKeyPeekBytes, context.RequestAborted);
            if (!string.IsNullOrWhiteSpace(operationKey))
            {
                var operation = await configuration.GetOperationAsync(operationKey, context.RequestAborted);
                if (operation is { MaxRequestSize: > 0 })
                {
                    var feature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
                    if (feature is not null && !feature.IsReadOnly)
                    {
                        feature.MaxRequestBodySize = operation.MaxRequestSize;
                    }
                }
            }

            context.Request.Body.Position = 0;
        }

        await _next(context);
    }

    private static async Task RejectTooLargeAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
        await context.Response.WriteAsJsonAsync(new { status = "PAYLOAD_TOO_LARGE", message = "Request body exceeds the global size limit." });
    }

    private static async Task<string?> TryPeekClientIdAsync(HttpRequest request, int maxPeekBytes, CancellationToken cancellationToken)
    {
        var buffer = new byte[maxPeekBytes];
        var total = 0;
        int read;
        while (total < maxPeekBytes &&
               (read = await request.Body.ReadAsync(buffer.AsMemory(total, maxPeekBytes - total), cancellationToken)) > 0)
        {
            total += read;
        }

        if (total == 0)
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(buffer.AsMemory(0, total));
            return doc.RootElement.ValueKind == JsonValueKind.Object &&
                   doc.RootElement.TryGetProperty("clientId", out var clientId)
                ? clientId.GetString()
                : null;
        }
        catch (JsonException)
        {
            // Body is larger than the peek window or not yet valid JSON; defer to the global/per-operation limits.
            return null;
        }
    }
}
