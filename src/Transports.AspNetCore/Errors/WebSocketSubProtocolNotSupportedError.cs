namespace GraphQL.Server.Transports.AspNetCore.Errors;

/// <summary>
/// Represents an error indicating that none of the requested websocket sub-protocols are supported.
/// </summary>
public class WebSocketSubProtocolNotSupportedError : RequestError
{
    /// <inheritdoc cref="WebSocketSubProtocolNotSupportedError"/>
    public WebSocketSubProtocolNotSupportedError(IEnumerable<string> requestedSubProtocols)
        : base($"Invalid requested WebSocket sub-protocol(s): {string.Join(",", requestedSubProtocols.Select(x => $"'{x}'"))}")
    {
    }
}
