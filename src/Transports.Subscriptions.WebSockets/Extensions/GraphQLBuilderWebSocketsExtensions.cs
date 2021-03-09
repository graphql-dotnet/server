using GraphQL.Server.Transports.Subscriptions.Abstractions;
using GraphQL.Server.Transports.WebSockets;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server
{
    public static class GraphQLBuilderWebSocketsExtensions
    {
        /// <summary>
        /// Add required services for GraphQL web sockets
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IGraphQLBuilder AddWebSockets(this IGraphQLBuilder builder)
        {
            builder.Services
                .AddTransient(typeof(IWebSocketConnectionFactory<>), typeof(WebSocketConnectionFactory<>))
                .AddTransient<IOperationMessageListener, LogMessagesListener>()
                .AddTransient<IOperationMessageListener, ProtocolMessageListener>();

            return builder;
        }
    }
}
