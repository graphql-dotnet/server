using System.Net;
using System.Net.WebSockets;
using System.Threading.Tasks;
using GraphQL.Transports.AspNetCore.Abstractions;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server.Transports.WebSockets
{
    public class WebSocketsTransport<TSchema> : ITransport<TSchema> where TSchema : Schema
    {
        /// <inheritdoc />
        public bool Accepts(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public async Task OnConnectedAsync(HttpContext context)
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
            
            var connection = new ConnectionContext(socket, context.Connection.Id);
            var endpoint = context.RequestServices.GetRequiredService<GraphQLEndPoint<TSchema>>();
            await endpoint.OnConnectedAsync(connection).ConfigureAwait(false);
            await endpoint.CloseConnectionAsync(connection).ConfigureAwait(false);
        }
    }
}
