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
        public bool AcceptsRequest(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                return false;
            }

            if (!context.WebSockets.WebSocketRequestedProtocols.Contains(GraphQLConnectionContext.Protocol))
            {
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public async Task AcceptAsync(HttpContext context)
        {
            var socket = await context.WebSockets
                .AcceptWebSocketAsync(GraphQLConnectionContext.Protocol);

            var connection = new GraphQLConnectionContext(socket, context.Connection.Id);
            var endpoint = context.RequestServices.GetRequiredService<GraphQLEndPoint<TSchema>>();
            await endpoint.OnConnectedAsync(connection);
        }
    }
}