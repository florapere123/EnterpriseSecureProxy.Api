namespace EnterpriseSecureProxy.Api.Infrastructure;

/// <summary>Turns a proxy status token into the matching HTTP status code.</summary>
internal static class ProxyStatusCodeMapper
{
    public static int ToHttpStatusCode(string status) => status switch
    {
        "OK" => StatusCodes.Status200OK,
        "VALIDATION_FAILED" => StatusCodes.Status400BadRequest,
        "IDEMPOTENCY_KEY_REQUIRED" => StatusCodes.Status400BadRequest,
        "DECRYPTION_FAILED" => StatusCodes.Status400BadRequest,
        "IDEMPOTENCY_KEY_REUSED_WITH_DIFFERENT_BODY" => StatusCodes.Status409Conflict,
        "NOT_FOUND" => StatusCodes.Status404NotFound,
        "FORBIDDEN" => StatusCodes.Status403Forbidden,
        "DESTINATION_UNAVAILABLE" => StatusCodes.Status503ServiceUnavailable,
        "DESTINATION_ERROR" => StatusCodes.Status502BadGateway,
        "PAYLOAD_TOO_LARGE" => StatusCodes.Status413PayloadTooLarge,
        _ => StatusCodes.Status500InternalServerError
    };
}
