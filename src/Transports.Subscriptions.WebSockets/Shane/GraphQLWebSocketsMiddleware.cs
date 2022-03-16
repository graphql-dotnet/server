#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Threading.Tasks;
using GraphQL.Server.Transports.AspNetCore;
using GraphQL.Server.Transports.Subscriptions.WebSockets.Shane;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GraphQL.Server.Transports.WebSockets.Shane
{
    /// <summary>
    /// ASP.NET Core middleware for processing GraphQL web socket requests. This middleware useful with and without ASP.NET Core routing.
    /// </summary>
    /// <typeparam name="TSchema">Type of GraphQL schema that is used to process requests.</typeparam>
    public class GraphQLWebSocketsMiddleware<TSchema> : IMiddleware
        where TSchema : ISchema
    {
        private readonly ILogger<GraphQLWebSocketsMiddleware<TSchema>> _logger;
        private readonly IDictionary<string, IWebSocketHandler> _handlers;
        private readonly IUserContextBuilder? _userContextBuilder;

        public GraphQLWebSocketsMiddleware(
            ILogger<GraphQLWebSocketsMiddleware<TSchema>> logger,
            IEnumerable<IWebSocketHandler<TSchema>> handlers)
            : this(logger, handlers, null)
        {
        }

        public GraphQLWebSocketsMiddleware(
            ILogger<GraphQLWebSocketsMiddleware<TSchema>> logger,
            IEnumerable<IWebSocketHandler<TSchema>> handlers,
            IUserContextBuilder? userContextBuilder)
        {
            _logger = logger;
            _userContextBuilder = userContextBuilder;
            _handlers = new Dictionary<string, IWebSocketHandler>();
            foreach (var handler in handlers.OrderBy(x => x.Priority))
            {
                foreach (var protocol in handler.SupportedSubProtocols)
                {
                    _handlers.TryAdd(protocol, handler);
                }
            }
        }

        public virtual async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["ConnectionId"] = context.Connection.Id,
                ["Request"] = context.Request
            }))
            {
                if (!context.WebSockets.IsWebSocketRequest)
                {
                    _logger.LogDebug("Request is not a valid websocket request");
                    await next(context);

                    return;
                }

                _logger.LogDebug("Connection is a valid websocket request");

                var (protocol, handler) = context.WebSockets.WebSocketRequestedProtocols
                    .Select(protocol => _handlers.TryGetValue(protocol, out var handler) ? (protocol, handler) : default)
                    .FirstOrDefault(x => x.protocol != null);

                if (protocol == null)
                {
                    _logger.LogError(
                        "Websocket connection does not have a supported protocol: {supported}. Request protocols: {requested}",
                        string.Join(", ", _handlers.Values.Select(x => $"'{x}'")),
                        string.Join(", ", context.WebSockets.WebSocketRequestedProtocols.Select(x => $"'{x}'")));

                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return;
                }

                var socket = await context.WebSockets.AcceptWebSocketAsync(protocol);

                if (socket.SubProtocol != protocol)
                {
                    _logger.LogError(
                        "Websocket has invalid protocol: '{expected}'. Requested: '{requested}'",
                        socket.SubProtocol,
                        protocol);

                    await socket.CloseAsync(
                        WebSocketCloseStatus.ProtocolError,
                        "Invalid protocol",
                        context.RequestAborted);
                }

                IDictionary<string, object?> userContext;
                if (_userContextBuilder == null)
                {
                    userContext = new Dictionary<string, object?>();
                }
                else
                {
                    userContext = await _userContextBuilder.BuildUserContext(context);
                }

                using (_logger.BeginScope($"GraphQL websocket connection: {context.Connection.Id}"))
                {
                    // Connect, then wait until the websocket has disconnected (and all subscriptions ended)
                    await handler.ExecuteAsync(context, socket, protocol, userContext);
                }
            }
        }
    }
}
