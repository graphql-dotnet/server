using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace GraphQL.Server.Transports.WebSockets
{
    public class GraphQLWebSocketsMiddleware<TSchema>
        where TSchema : ISchema
    {
        private readonly RequestDelegate _next;
        private readonly PathString _path;
        private readonly ILogger<GraphQLWebSocketsMiddleware<TSchema>> _logger;

        public GraphQLWebSocketsMiddleware(RequestDelegate next, PathString path, ILogger<GraphQLWebSocketsMiddleware<TSchema>> logger)
        {
            _next = next;
            _path = path;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["ConnectionId"] = context.Connection.Id,
                ["Request"] = context.Request
            }))
            {
                if (!context.WebSockets.IsWebSocketRequest || !context.Request.Path.StartsWithSegments(_path))
                {
                    _logger.LogDebug("Request is not a valid websocket request");
                    await _next(context);

                    return;
                }

                _logger.LogDebug("Connection is a valid websocket request");

                var socket = await context.WebSockets.AcceptWebSocketAsync("graphql-ws");

                if (!context.WebSockets.WebSocketRequestedProtocols.Contains(socket.SubProtocol))
                {
                    _logger.LogError(
                        "Websocket connection does not have correct protocol: graphql-ws. Request protocols: {protocols}",
                        context.WebSockets.WebSocketRequestedProtocols);

                    await socket.CloseAsync(
                        WebSocketCloseStatus.ProtocolError,
                        "Server only supports graphql-ws protocol",
                        context.RequestAborted);

                    return;
                }

                using (_logger.BeginScope($"GraphQL websocket connection: {context.Connection.Id}"))
                {
                    var connectionFactory = context.RequestServices.GetRequiredService<IWebSocketConnectionFactory<TSchema>>();
                    var connection = connectionFactory.CreateConnection(socket, context.Connection.Id);

                    await connection.Connect();
                }
            }
        }
    }
}
