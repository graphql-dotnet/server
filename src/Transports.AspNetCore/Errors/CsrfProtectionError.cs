namespace GraphQL.Server.Transports.AspNetCore.Errors;

/// <summary>
/// Represents an error indicating that the request may not have triggered a CORS preflight request.
/// </summary>
public class CsrfProtectionError : RequestError
{
    /// <inheritdoc cref="CsrfProtectionError"/>
    public CsrfProtectionError(IEnumerable<string> headersRequired) : base($"This request requires a non-empty header from the following list: {FormatHeaders(headersRequired)}.") { }

    /// <inheritdoc cref="CsrfProtectionError"/>
    public CsrfProtectionError(IEnumerable<string> headersRequired, Exception innerException) : base($"This request requires a non-empty header from the following list: {FormatHeaders(headersRequired)}. {innerException.Message}") { }

    private static string FormatHeaders(IEnumerable<string> headersRequired)
        => string.Join(", ", headersRequired.Select(x => $"'{x}'"));
}
