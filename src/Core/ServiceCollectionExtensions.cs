using System;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Execution;
using GraphQL.Http;
using GraphQL.Server.Core;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IGraphQLBuilder AddGraphQL(this IServiceCollection services, GraphQLOptions options)
        {
            services.AddSingleton<IDocumentExecuter, DocumentExecuter>();
            services.AddTransient(typeof(IGraphQLExecuter<>), typeof(DefaultSchemaExecuter<>));
            services.AddSingleton(Options.Options.Create(options));

            services.TryAddSingleton<IDocumentWriter>(x =>
            {
                var jsonSerializerSettings = x.GetRequiredService<IOptions<JsonSerializerSettings>>();
                return new DocumentWriter(Formatting.None, jsonSerializerSettings.Value);
            });

            return new GraphQLBuilder(services);
        }

        public static IGraphQLBuilder AddGraphQL(this IServiceCollection services, Action<GraphQLOptions> configureOptions)
        {
            var options = new GraphQLOptions();
            configureOptions(options);

            return services.AddGraphQL(options);
        }

        public static IGraphQLBuilder AddGraphQL(this IServiceCollection services)
        {
            return services.AddGraphQL(new GraphQLOptions());
        }

        public static IGraphQLBuilder AddDataLoader(this IGraphQLBuilder builder)
        {
            builder.Services.AddSingleton<IDataLoaderContextAccessor, DataLoaderContextAccessor>();
            builder.Services.AddSingleton<IDocumentExecutionListener, DataLoaderDocumentListener>();

            return builder;
        }
    }
}
