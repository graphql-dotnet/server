using GraphQL.Http;
using GraphQL.Subscription;
using GraphQL.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace GraphQL.Server.Transports.AspNetCore
{
    public static class GraphQlExtensions
    {
        public static IServiceCollection AddGraphQLHttp(this IServiceCollection services)
        {
            services.TryAddSingleton<IDocumentWriter, DocumentWriter>();
            services.TryAddSingleton<IDocumentExecuter, DocumentExecuter>();
            services.TryAddSingleton<ISubscriptionExecuter, SubscriptionExecuter>();

            return services;
        }

        public static IApplicationBuilder UseGraphQLHttp<TSchema>(this IApplicationBuilder builder,
            GraphQlOptions schemaOptions)
            where TSchema : ISchema
        {
            builder.UseMiddleware<GraphQlHttpMiddleware<TSchema>>(Options.Create(schemaOptions));

            return builder;
        }
    }
}
