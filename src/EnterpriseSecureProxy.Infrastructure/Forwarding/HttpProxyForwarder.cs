using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseSecureProxy.Application.Dtos;
using EnterpriseSecureProxy.Application.Interfaces;
using EnterpriseSecureProxy.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace EnterpriseSecureProxy.Infrastructure.Forwarding;

/// <summary>
/// <see cref="IProxyForwarder"/> backed by <see cref="HttpClient"/>. Builds the outbound request
/// from the fully-resolved <see cref="ForwardRequest"/>, applies the resolved header allowlist, and
/// normalizes the downstream response. Resilience policies (timeout/retry/circuit-breaker) are
/// attached to the named client in DI, so this type stays focused on request shaping.
/// </summary>
public sealed class HttpProxyForwarder : IProxyForwarder
{
    public const string HttpClientName = "SecureProxyForwarder";

    private const int MaxDownstreamResponseBytes = 10 * 1024 * 1024;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DestinationConcurrencyLimiter _concurrency;
    private readonly ILogger<HttpProxyForwarder> _logger;

    public HttpProxyForwarder(
        IHttpClientFactory httpClientFactory,
        DestinationConcurrencyLimiter concurrency,
        ILogger<HttpProxyForwarder> logger)
    {
        _httpClientFactory = httpClientFactory;
        _concurrency = concurrency;
        _logger = logger;
    }

    public async Task<ForwardResult> ForwardAsync(ForwardRequest request, CancellationToken cancellationToken = default)
    {
        using var lease = await _concurrency.AcquireAsync(request.Destination.Name, cancellationToken);
        if (!lease.Acquired)
        {
            return new ForwardResult
            {
                IsSuccess = false,
                StatusCode = StatusDestinationAtCapacity,
                FailureReason = "The downstream destination is at capacity. Retry later."
            };
        }

        var client = _httpClientFactory.CreateClient(HttpClientName);

        using var message = new HttpRequestMessage(ToHttpMethod(request.Operation.Method), request.TargetUri);
        message.Options.Set(ForwarderHttpRequestOptions.AllowHttpRetry, IsHttpRetryAllowed(request));

        foreach (var (name, value) in request.Headers)
        {
            // TryAddWithoutValidation keeps the allowlist authoritative without framework header policing.
            message.Headers.TryAddWithoutValidation(name, value);
        }

        if (request.Body is { } body && request.Operation.Method is not HttpVerb.Get and not HttpVerb.Delete)
        {
            message.Content = JsonContent.Create(body, options: SerializerOptions);
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var timeout = request.Operation.Timeout ?? request.Destination.Timeout;
            cts.CancelAfter(timeout);

            using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            var payload = await ReadJsonAsync(response, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Downstream {Destination} returned {StatusCode} for operation {OperationKey} (correlation {CorrelationId}).",
                    request.Destination.Name, (int)response.StatusCode, request.Operation.OperationKey, request.CorrelationId);

                return new ForwardResult
                {
                    IsSuccess = false,
                    StatusCode = (int)response.StatusCode,
                    Body = payload,
                    FailureReason = $"Downstream service returned status {(int)response.StatusCode}."
                };
            }

            return new ForwardResult
            {
                IsSuccess = true,
                StatusCode = (int)response.StatusCode,
                Body = payload
            };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError("Downstream {Destination} timed out after {Timeout}s (correlation {CorrelationId}).",
                request.Destination.Name, (request.Operation.Timeout ?? request.Destination.Timeout).TotalSeconds, request.CorrelationId);

            return new ForwardResult
            {
                IsSuccess = false,
                StatusCode = StatusGatewayTimeout,
                FailureReason = "The downstream service timed out."
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Transport failure calling {Destination} (correlation {CorrelationId}).",
                request.Destination.Name, request.CorrelationId);

            return new ForwardResult
            {
                IsSuccess = false,
                StatusCode = StatusBadGateway,
                FailureReason = "The downstream service could not be reached."
            };
        }
    }

    private static async Task<JsonElement?> ReadJsonAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength is 0)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        var buffer = new char[Math.Min(MaxDownstreamResponseBytes, 8192)];
        var builder = new System.Text.StringBuilder();
        int read;
        while ((read = await reader.ReadAsync(buffer, cancellationToken)) > 0)
        {
            builder.Append(buffer, 0, read);
            if (builder.Length > MaxDownstreamResponseBytes)
            {
                throw new InvalidOperationException("Downstream response exceeded the configured size limit.");
            }
        }

        var raw = builder.ToString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            // Non-JSON downstream payloads are surfaced as a JSON string so the envelope stays well-formed.
            return JsonSerializer.SerializeToElement(raw);
        }
    }

    /// <summary>
    /// GET/HEAD/OPTIONS may retry on transient failures. POST/PUT/PATCH never retry here — duplication
    /// protection for POST relies on <c>Idempotency-Key</c> via <c>IdempotencyStep</c>.
    /// DELETE retries only when explicitly flagged on the operation.
    /// </summary>
    private static bool IsHttpRetryAllowed(ForwardRequest request) => request.Operation.Method switch
    {
        HttpVerb.Get => true,
        HttpVerb.Delete => request.Operation.AllowHttpClientRetryForDelete,
        _ => false
    };

    private static HttpMethod ToHttpMethod(HttpVerb verb) => verb switch
    {
        HttpVerb.Get => HttpMethod.Get,
        HttpVerb.Post => HttpMethod.Post,
        HttpVerb.Put => HttpMethod.Put,
        HttpVerb.Patch => HttpMethod.Patch,
        HttpVerb.Delete => HttpMethod.Delete,
        _ => HttpMethod.Post
    };

    private const int StatusBadGateway = 502;
    private const int StatusGatewayTimeout = 504;
    private const int StatusDestinationAtCapacity = 503;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
}
