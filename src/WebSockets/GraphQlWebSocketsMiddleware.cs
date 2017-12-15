using System.Net.WebSockets;
using System.Threading.Tasks;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace GraphQL.Server.Transports.WebSockets
{
    public class GraphQLWebSocketsMiddleware<TSchema> where TSchema : ISchema
    {
        private readonly RequestDelegate _next;
        private readonly GraphQLWebSocketsOptions _options;
        private readonly GraphQLEndPoint<TSchema> _endpoint;

        public GraphQLWebSocketsMiddleware(
            RequestDelegate next,
            IOptions<GraphQLWebSocketsOptions> options,
            GraphQLEndPoint<TSchema> endpoint)
        {
            _next = next;
            _options = options.Value;
            _endpoint = endpoint;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!IsGraphQlRequest(context))
            {
                await _next(context);
                return;
            }

            await ExecuteAsync(context);
        }

        private bool IsGraphQlRequest(HttpContext context)
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
                .AcceptWebSocketAsync(ConnectionContext.Protocol).ConfigureAwait(false);

            if (!context.WebSockets.WebSocketRequestedProtocols
                .Contains(socket.SubProtocol))
            {
                await socket.CloseAsync(
                    WebSocketCloseStatus.ProtocolError,
                    $"Server only supports {ConnectionContext.Protocol} protocol",
                    context.RequestAborted).ConfigureAwait(false);

                return;
            }
            
            var connection = new ConnectionContext(socket, context.Connection.Id, _options);
            await _endpoint.OnConnectedAsync(connection).ConfigureAwait(false);
            await _endpoint.CloseConnectionAsync(connection).ConfigureAwait(false);
        }
    }
}
