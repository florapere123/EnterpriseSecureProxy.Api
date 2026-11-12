namespace EnterpriseSecureProxy.Infrastructure.Services;

/// <summary>Outcome of decoding a Base64 RowVersion: the bytes, or why it failed.</summary>
public readonly record struct RowVersionParseResult(bool Success, byte[]? Bytes, string ErrorMessage)
{
    public static RowVersionParseResult Missing { get; } =
        new(false, null, "RowVersion is required for optimistic concurrency.");

    public static RowVersionParseResult InvalidBase64 { get; } =
        new(false, null, "RowVersion must be valid Base64.");

    public static RowVersionParseResult Ok(byte[] bytes) => new(true, bytes, string.Empty);
}

/// <summary>Decodes the Base64 RowVersion that admin callers send back on updates.</summary>
internal static class RowVersionParser
{
    public static RowVersionParseResult TryParse(string? rowVersionBase64)
    {
        if (string.IsNullOrWhiteSpace(rowVersionBase64))
        {
            return RowVersionParseResult.Missing;
        }

        try
        {
            return RowVersionParseResult.Ok(Convert.FromBase64String(rowVersionBase64));
        }
        catch (FormatException)
        {
            return RowVersionParseResult.InvalidBase64;
        }
    }
}
