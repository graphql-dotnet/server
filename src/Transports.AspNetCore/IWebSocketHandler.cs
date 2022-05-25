using System.Net.WebSockets;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Transports.AspNetCore;

/// <inheritdoc/>
public interface IWebSocketHandler<TSchema> : IWebSocketHandler
    where TSchema : ISchema
{
}

/// <summary>
/// Handles a WebSocket connection based on the sub-protocol specified.
/// </summary>
public interface IWebSocketHandler
{
    /// <summary>
    /// Executes a specified WebSocket request, returning once the connection is closed.
    /// </summary>
    Task ExecuteAsync(HttpContext httpContext, WebSocket webSocket, string subProtocol, IUserContextBuilder userContextBuilder);

    /// <summary>
    /// Gets a list of WebSocket sub-protocols supported by this handler.
    /// </summary>
    IEnumerable<string> SupportedSubProtocols { get; }
}
