using System;
using GraphQL.Instrumentation;
using GraphQL.Server.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GraphQL.Server
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add required services for GraphQL.
        /// </summary>
        /// <param name="services"></param>
        /// <returns>GraphQL builder used for GraphQL specific extension chaining.</returns>
        public static IGraphQLBuilder AddGraphQL(this IServiceCollection services) => services.AddGraphQL(_ => { });

        /// <summary>
        /// Add required services for GraphQL.
        /// <br/><br/>
        /// If dependencies are required to configure options see https://andrewlock.net/simplifying-dependency-injection-for-iconfigureoptions-with-the-configureoptions-helper/#using-services-to-configure-strongly-typed-options
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configureOptions">An action delegate to configure the provided <see cref="GraphQLOptions"/>.</param>
        /// <returns>GraphQL builder used for GraphQL specific extension chaining.</returns>
        public static IGraphQLBuilder AddGraphQL(this IServiceCollection services, Action<GraphQLOptions> configureOptions)
        {
            services.Configure(configureOptions ?? throw new ArgumentNullException(nameof(configureOptions)));
            services.TryAddSingleton<InstrumentFieldsMiddleware>();
            services.TryAddSingleton<IDocumentExecuter, DocumentExecuter>();
            services.TryAddTransient(typeof(IGraphQLExecuter<>), typeof(DefaultGraphQLExecuter<>));

            services.TryAddSingleton<IDocumentWriter>(x =>
            {
                throw new InvalidOperationException(
                    "IDocumentWriter not set in DI container. " +
                    "Add a IDocumentWriter implementation, for example " +
                    "GraphQL.SystemTextJson.DocumentWriter or GraphQL.NewtonsoftJson.DocumentWriter." +
                    "For more information, see: https://github.com/graphql-dotnet/graphql-dotnet/blob/master/README.md and https://github.com/graphql-dotnet/server/blob/develop/README.md.");
            });

            return new GraphQLBuilder(services);
        }
    }
}
