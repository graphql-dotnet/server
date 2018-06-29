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
    public class GraphQLWebSocketsMiddleware<TSchema> : IMiddleware
        where TSchema : ISchema
    {
        private readonly IEnumerable<IOperationMessageListener> _messageListeners;
        private readonly IGraphQLExecuter<TSchema> _executer;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        public GraphQLWebSocketsMiddleware(
            IEnumerable<IOperationMessageListener> messageListeners,
            IGraphQLExecuter<TSchema> executer,
            ILoggerFactory loggerFactory)
        {
            _messageListeners = messageListeners;
            _executer = executer;

            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<GraphQLWebSocketsMiddleware<TSchema>>();
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["ConnectionId"] = context.Connection.Id,
                ["Request"] = context.Request
            }))
            {
                if (!context.WebSockets.IsWebSocketRequest)
                {
                    _logger.LogInformation("Request is not a valid  websocket request");
                    await next(context);
                    return;
                }

                _logger.LogInformation("Connection is a valid websocket request");
                await ExecuteAsync(context);
            }
        }

        private async Task ExecuteAsync(HttpContext context)
        {
            var socket = await context.WebSockets
                .AcceptWebSocketAsync("graphql-ws").ConfigureAwait(false);

            if (!context.WebSockets.WebSocketRequestedProtocols
                .Contains(socket.SubProtocol))
            {
                _logger.LogError(
                    "Websocket connection does not have correct protocol: graphql-ws. Request protocols: {protocols}",
                    context.WebSockets.WebSocketRequestedProtocols);
                await socket.CloseAsync(
                    WebSocketCloseStatus.ProtocolError,
                    $"Server only supports graphql-ws protocol",
                    context.RequestAborted).ConfigureAwait(false);

                return;
            }

            using (_logger.BeginScope($"GraphQL websocket connection: {context.Connection.Id}"))
            {
                var connection = new WebSocketConnection(
                    socket,
                    context.Connection.Id,
                    new SubscriptionManager(_executer, _loggerFactory),
                    _messageListeners,
                    _loggerFactory);

                await connection.Connect();
            }
        }
    }
}