using System;
using GraphQL.Http;
using GraphQL.Server.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace GraphQL.Server
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add required services for GraphQL
        /// </summary>
        /// <param name="services"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IGraphQLBuilder AddGraphQL(this IServiceCollection services, GraphQLOptions options)
        {
            services.TryAddSingleton<IDocumentExecuter, DocumentExecuter>();
            services.AddTransient(typeof(IGraphQLExecuter<>), typeof(DefaultGraphQLExecuter<>));
            services.AddSingleton(Options.Create(options));

            services.TryAddSingleton<IDocumentWriter>(x =>
            {
                var jsonSerializerSettings = x.GetRequiredService<IOptions<JsonSerializerSettings>>();
                return new DocumentWriter(Formatting.None, jsonSerializerSettings.Value);
            });

            return new GraphQLBuilder(services);
        }

        /// <summary>
        /// Add required services for GraphQL
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        public static IGraphQLBuilder AddGraphQL(this IServiceCollection services, Action<GraphQLOptions> configureOptions)
        {
            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));

            var options = new GraphQLOptions();
            configureOptions(options);

            return services.AddGraphQL(options);
        }

        /// <summary>
        /// Add required services for GraphQL
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IGraphQLBuilder AddGraphQL(this IServiceCollection services)
        {
            return services.AddGraphQL(new GraphQLOptions());
        }
    }
}
