using GraphQL.Http;using GraphQL.Server.Transports.WebSockets.Abstractions;
using GraphQL.Subscription;
using GraphQL.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace GraphQL.Server.Transports.WebSockets
{
    public static class GraphQLWebSocketsExtensions
    {
        public static IServiceCollection AddGraphQLWebSocket<TSchema>(this IServiceCollection services) where TSchema : ISchema
        {
            services.TryAddSingleton<ISubscriptionExecuter, SubscriptionExecuter>();
            services.TryAddSingleton<IDocumentWriter, DocumentWriter>();

            services.TryAddSingleton<ISubscriptionProtocolHandler<TSchema>, SubscriptionProtocolHandler<TSchema>>();
            services.TryAddSingleton<GraphQLEndPoint<TSchema>>();

            return services;
        }

        public static IApplicationBuilder UseGraphQLWebSocket<TSchema>(this IApplicationBuilder builder,
            GraphQLWebSocketsOptions schemaOptions)
            where TSchema : ISchema
        {
            builder.UseMiddleware<GraphQLWebSocketsMiddleware<TSchema>>(Options.Create(schemaOptions));
            return builder;
        }
    }
}
