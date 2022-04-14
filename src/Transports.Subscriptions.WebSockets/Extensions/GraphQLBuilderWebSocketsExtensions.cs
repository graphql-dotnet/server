using GraphQL.DI;
using GraphQL.Server.Transports.Subscriptions.Abstractions;
using GraphQL.Server.Transports.WebSockets;
using GraphQL.Types;

namespace GraphQL.Server;

public static class GraphQLBuilderWebSocketsExtensions
{
    /// <summary>
    /// Add required services for GraphQL web sockets
    /// </summary>
    public static IGraphQLBuilder AddWebSockets(this IGraphQLBuilder builder)
    {
        builder.Services
            .Register(typeof(IWebSocketConnectionFactory<>), typeof(WebSocketConnectionFactory<>), ServiceLifetime.Transient)
            .Register<IOperationMessageListener, LogMessagesListener>(ServiceLifetime.Transient)
            .Register<IOperationMessageListener, ProtocolMessageListener>(ServiceLifetime.Transient);

        return builder;
    }

    public static IGraphQLBuilder AddWebSocketsHttpMiddleware<TSchema>(this IGraphQLBuilder builder)
        where TSchema : ISchema
    {
        builder.Services.Register<GraphQLWebSocketsMiddleware<TSchema>, GraphQLWebSocketsMiddleware<TSchema>>(ServiceLifetime.Singleton);
        return builder;
    }

    public static IGraphQLBuilder AddWebSocketsHttpMiddleware<TSchema, TMiddleware>(this IGraphQLBuilder builder)
        where TSchema : ISchema
        where TMiddleware : GraphQLWebSocketsMiddleware<TSchema>
    {
        builder.Services.Register<TMiddleware, TMiddleware>(ServiceLifetime.Singleton);
        return builder;
    }
}
