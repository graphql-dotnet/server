using GraphQL.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace GraphQL.Server.Transports.WebSockets
{
    public static class GraphQLWebSocketsExtensions
    {
        public static IServiceCollection AddGraphQLWebSocket<TSchema>(this IServiceCollection services)
            where TSchema : ISchema
        {
            services.TryAddSingleton<IDocumentExecuter, DocumentExecuter>();
            services.TryAddSingleton<IGraphQLExecuterFactory<TSchema>, ConfigurableExecuterFactory<TSchema>>();
            services.TryAddSingleton<ConfigurableExecuter<TSchema>>();
            services.AddSingleton<IConfigureOptions<ExecutionOptions<TSchema>>, AddDefaultMessageListeners<TSchema>>();
            services.AddSingleton<IConfigureOptions<ExecutionOptions<TSchema>>, UseConfigurableExecuterFactory<TSchema>>();
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