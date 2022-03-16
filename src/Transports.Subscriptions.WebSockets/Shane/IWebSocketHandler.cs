#nullable enable

using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Transports.Subscriptions.WebSockets.Shane;

public interface IWebSocketHandler<TSchema> : IWebSocketHandler
    where TSchema : ISchema
{
}

public interface IWebSocketHandler
{
    Task ExecuteAsync(HttpContext httpContext, WebSocket webSocket, string subProtocol, IDictionary<string, object?> userContext);
    IEnumerable<string> SupportedSubProtocols { get; }
    int Priority { get; }
}
