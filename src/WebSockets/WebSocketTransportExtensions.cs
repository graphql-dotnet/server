using GraphQL.Server.Transports.WebSockets.Abstractions;
using GraphQL.Transports.AspNetCore.Abstractions;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server.Transports.WebSockets
{
    public static class WebSocketTransportExtensions
    {
        public static IServiceCollection AddGraphQLWebSocketsTransport<TSchema>(this IServiceCollection services) where TSchema : Schema
        {
            services.AddSingleton<ISubscriptionProtocolHandler<TSchema>, SubscriptionProtocolHandler<TSchema>>();
            services.AddSingleton<GraphQLEndPoint<TSchema>>();
            services.AddSingleton<ITransport<TSchema>, WebSocketsTransport<TSchema>>();
            return services;
        }
    }
}
