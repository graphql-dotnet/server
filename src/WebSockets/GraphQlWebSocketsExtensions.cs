using GraphQL.Http;using GraphQL.Server.Transports.WebSockets.Abstractions;
using GraphQL.Subscription;
using GraphQL.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace GraphQL.Server.Transports.WebSockets
{
    public static class GraphQlWebSocketsExtensions
    {
        public static IServiceCollection AddGraphQlWebSockets<TSchema>(this IServiceCollection services) where TSchema : ISchema
        {
            services.TryAddSingleton<ISubscriptionExecuter, SubscriptionExecuter>();
            services.TryAddSingleton<IDocumentWriter, DocumentWriter>();

            services.TryAddSingleton<ISubscriptionProtocolHandler<TSchema>, SubscriptionProtocolHandler<TSchema>>();
            services.TryAddSingleton<GraphQlEndPoint<TSchema>>();

            return services;
        }

        public static IApplicationBuilder UseGraphQlEndPoint<TSchema>(this IApplicationBuilder builder,
            GraphQlWebSocketsOptions schemaOptions)
            where TSchema : ISchema
        {
            builder.UseMiddleware<GraphQlWebSocketsMiddleware<TSchema>>(Options.Create(schemaOptions));
            return builder;
        }
    }
}
