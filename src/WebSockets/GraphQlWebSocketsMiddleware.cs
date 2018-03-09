using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using GraphQL.Server.Transports.Subscriptions.Abstractions;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace GraphQL.Server.Transports.WebSockets
{
    public class GraphQLWebSocketsMiddleware<TSchema> where TSchema : ISchema
    {
        private readonly RequestDelegate _next;
        private readonly GraphQLWebSocketsOptions _options;
        private readonly IGraphQLExecuter _executer;

        public GraphQLWebSocketsMiddleware(
            RequestDelegate next,
            IGraphQLExecuterFactory executerFactory,
            IOptions<GraphQLWebSocketsOptions> options)
        {
            _next = next;
            _executer = executerFactory.Create<TSchema>();
            _options = options.Value;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!IsGraphQLRequest(context))
            {
                await _next(context);
                return;
            }

            await ExecuteAsync(context);
        }

        private bool IsGraphQLRequest(HttpContext context)
        {
            if (!context.Request.Path.StartsWithSegments(_options.Path))
            {
                return false;
            }

            if (!context.WebSockets.IsWebSocketRequest)
            {
                return false;
            }

            return true;
        }

        private async Task ExecuteAsync(HttpContext context)
        {
            var socket = await context.WebSockets
                .AcceptWebSocketAsync("graphql-ws").ConfigureAwait(false);

            if (!context.WebSockets.WebSocketRequestedProtocols
                .Contains(socket.SubProtocol))
            {
                await socket.CloseAsync(
                    WebSocketCloseStatus.ProtocolError,
                    $"Server only supports graphql-ws protocol",
                    context.RequestAborted).ConfigureAwait(false);

                return;
            }

            var connection = new WebSocketConnection(
                socket, 
                context.Connection.Id,
                new SubscriptionManager(_executer));

            await connection.Connect();
            await connection.Close();
        }
    }
}
