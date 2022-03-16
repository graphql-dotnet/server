#nullable enable

using GraphQL.DI;
using GraphQL.Server.Transports.AspNetCore;
using GraphQL.Server.Transports.AspNetCore.WebSockets;

namespace GraphQL.Server
{
    /// <summary>
    /// GraphQL specific extension methods for <see cref="IGraphQLBuilder"/>.
    /// </summary>
    public static class GraphQLBuilderWebSocketsExtensions
    {
        /// <summary>
        /// Add required services for GraphQL web sockets
        /// </summary>
        public static IGraphQLBuilder AddWebSockets(this IGraphQLBuilder builder)
        {
            builder.Services
                .Register(typeof(IWebSocketHandler<>), typeof(WebSocketHandler<>), DI.ServiceLifetime.Singleton)
                .Configure<WebSocketHandlerOptions>();

            return builder;
        }
    }
}
