namespace EnterpriseSecureProxy.Application.Validation;

/// <summary>Guards against path traversal in route/path template substitution values.</summary>
internal static class PathParameterSecurity
{
   // URL-encoded traversal sequences:
   private static readonly string[] EncodedTraversalFragments =
    {
            "%2e%2e", // ".." (parent directory)
            "%2f",    // "/"  (forward slash)
            "%5c"     // "\"  (backslash)
    };

    // OWASP Broken Access Control:
    // block raw or URL-encoded traversal (e.g. .. or /)
    // so values cannot escape the template.
    public static bool ContainsTraversalAttempt(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        if (ContainsRawTraversal(value))
        {
            return true;
        }

        string decoded;
        try
        {
            decoded = Uri.UnescapeDataString(value);
        }
        catch (UriFormatException)
        {
            return true;
        }

        return !string.Equals(decoded, value, StringComparison.Ordinal) && ContainsRawTraversal(decoded);
    }

    private static bool ContainsRawTraversal(string value)
    {
        if (value.Contains("..", StringComparison.Ordinal))
        {
            return true;
        }

        if (value.Contains('/') || value.Contains('\\'))
        {
            return true;
        }

        foreach (var fragment in EncodedTraversalFragments)
        {
            if (value.Contains(fragment, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
