using GraphQL.Http;
using GraphQL.Subscription;
using GraphQL.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace GraphQL.Server.Transports.AspNetCore
{
    public static class GraphQLExtensions
    {
        /// <summary>
        /// Adds the GraphQLHttp to services
        /// </summary>
        /// <param name="services">The servicecollection to registrer the services on</param>
        /// <returns>The <see cref="IServiceCollection"/> recieved as parameter</returns>
        public static IServiceCollection AddGraphQLHttp(this IServiceCollection services)
        {
            services.TryAddSingleton<IDocumentWriter, DocumentWriter>();
            services.TryAddSingleton<IDocumentExecuter, DocumentExecuter>();
            services.TryAddSingleton<ISubscriptionExecuter, SubscriptionExecuter>();

            return services;
        }

        /// <summary>
        /// Adds the GraphQLHttp to services
        /// </summary>
        /// <typeparam name="TUserContextBuilder">The <see cref="IUserContextBuilder"/> to use for generating the userContext used for the GraphQL request</typeparam>
        /// <param name="services">The servicecollection to registrer the services on</param>
        /// <returns>The <see cref="IServiceCollection"/> recieved as parameter</returns>
        public static IServiceCollection AddGraphQLHttp<TUserContextBuilder>(this IServiceCollection services)
            where TUserContextBuilder : class, IUserContextBuilder
        {
            services.AddGraphQLHttp();
            services.TryAddTransient<IUserContextBuilder, TUserContextBuilder>();

            return services;
        }

        public static IApplicationBuilder UseGraphQLHttp<TSchema>(this IApplicationBuilder builder,
            GraphQLHttpOptions schemaOptions)
            where TSchema : ISchema
        {
            builder.UseMiddleware<GraphQLHttpMiddleware<TSchema>>(Options.Create(schemaOptions));

            return builder;
        }
    }
}
