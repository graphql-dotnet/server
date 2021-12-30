using GraphQL.DI;
using GraphQL.Server.Transports.Subscriptions.Abstractions;
using GraphQL.Server.Transports.WebSockets;

namespace GraphQL.Server
{
    public static class GraphQLBuilderWebSocketsExtensions
    {
        /// <summary>
        /// Add required services for GraphQL web sockets
        /// </summary>
        public static IGraphQLBuilder AddWebSockets(this IGraphQLBuilder builder)
        {
            builder.Services
                .Register(typeof(IWebSocketConnectionFactory<>), typeof(WebSocketConnectionFactory<>), DI.ServiceLifetime.Transient)
                .Register<IOperationMessageListener, LogMessagesListener>(DI.ServiceLifetime.Transient)
                .Register<IOperationMessageListener, ProtocolMessageListener>(DI.ServiceLifetime.Transient);

            return builder;
        }
    }
}
