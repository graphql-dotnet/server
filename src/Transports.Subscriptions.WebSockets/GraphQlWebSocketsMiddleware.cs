using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using GraphQL.Server.Core;
using GraphQL.Server.Transports.Subscriptions.Abstractions;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GraphQL.Server.Transports.WebSockets
{
    public class GraphQLWebSocketsMiddleware<TSchema>
        where TSchema : ISchema
    {
        private readonly RequestDelegate _next;
 
        public GraphQLWebSocketsMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, 
            IEnumerable<IOperationMessageListener> messageListeners,
            IGraphQLExecuter<TSchema> executer,
            ILoggerFactory loggerFactory,
            ILogger<GraphQLWebSocketsMiddleware<TSchema>> logger)
        {
            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["ConnectionId"] = context.Connection.Id,
                ["Request"] = context.Request
            }))
            {
                if (!context.WebSockets.IsWebSocketRequest)
                {
                    logger.LogInformation("Request is not a valid  websocket request");
                    await _next(context);

                    return;
                }

                logger.LogInformation("Connection is a valid websocket request");
                var socket = await context.WebSockets
                    .AcceptWebSocketAsync("graphql-ws").ConfigureAwait(false);

                if (!context.WebSockets.WebSocketRequestedProtocols
                    .Contains(socket.SubProtocol))
                {
                    logger.LogError(
                        "Websocket connection does not have correct protocol: graphql-ws. Request protocols: {protocols}",
                        context.WebSockets.WebSocketRequestedProtocols);
                    await socket.CloseAsync(
                        WebSocketCloseStatus.ProtocolError,
                        $"Server only supports graphql-ws protocol",
                        context.RequestAborted).ConfigureAwait(false);

                    return;
                }

                using (logger.BeginScope($"GraphQL websocket connection: {context.Connection.Id}"))
                {
                    var connection = new WebSocketConnection(
                        socket,
                        context.Connection.Id,
                        new SubscriptionManager(executer, loggerFactory),
                        messageListeners,
                        loggerFactory);

                    await connection.Connect();
                }
            }
        }
    }
}