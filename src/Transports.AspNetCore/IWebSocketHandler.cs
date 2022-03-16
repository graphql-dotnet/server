#nullable enable

using System.Net.WebSockets;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Transports.AspNetCore;

public interface IWebSocketHandler<TSchema> : IWebSocketHandler
    where TSchema : ISchema
{
}

public interface IWebSocketHandler
{
    Task ExecuteAsync(HttpContext httpContext, WebSocket webSocket, string subProtocol, IDictionary<string, object?> userContext, CancellationToken cancellationToken);
    IEnumerable<string> SupportedSubProtocols { get; }
}
