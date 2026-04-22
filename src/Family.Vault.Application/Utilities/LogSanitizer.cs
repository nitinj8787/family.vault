namespace Family.Vault.Application.Utilities;

/// <summary>
/// Helpers for sanitizing user-supplied values before they are written to log sinks,
/// preventing log-forging / CRLF-injection attacks.
/// </summary>
public static class LogSanitizer
{
    /// <summary>
    /// Strips control characters (newlines, carriage returns, tabs) from <paramref name="value"/>
    /// and replaces them with underscores so that a malicious value cannot inject extra log lines.
    /// Returns an empty string when <paramref name="value"/> is <see langword="null"/>.
    /// </summary>
    public static string Sanitize(string? value) =>
        value is null
            ? string.Empty
            : value.Replace('\n', '_').Replace('\r', '_').Replace('\t', '_');
}
