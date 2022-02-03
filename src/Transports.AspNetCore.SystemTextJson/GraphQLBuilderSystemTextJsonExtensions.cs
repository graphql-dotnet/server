using System;
using System.Linq;
using System.Text.Json;
using GraphQL.DI;
using GraphQL.Server.Transports.AspNetCore;
using GraphQL.Server.Transports.AspNetCore.SystemTextJson;

namespace GraphQL.Server
{
    public static class GraphQLBuilderSystemTextJsonExtensions
    {
        /// <summary>
        /// Adds a <see cref="IGraphQLRequestDeserializer"/> and a <see cref="IDocumentWriter"/>
        /// to the service collection with the provided configuration/settings.
        /// </summary>
        /// <param name="builder">The <see cref="IGraphQLBuilder"/>.</param>
        /// <param name="configureDeserializerSettings">
        /// Action to further configure the request deserializer's settings.
        /// Affects reading of the JSON from the HTTP request the middleware processes.
        /// </param>
        /// <param name="configureSerializerSettings">
        /// Action to further configure the response serializer's settings.
        /// Affects JSON returned by the middleware.
        /// </param>
        /// <returns>GraphQL Builder.</returns>
        public static IGraphQLBuilder AddSystemTextJson(this IGraphQLBuilder builder,
            Action<JsonSerializerOptions> configureDeserializerSettings = null,
            Action<JsonSerializerOptions> configureSerializerSettings = null)
        {
            builder.Services.Register<IGraphQLRequestDeserializer>(services => new GraphQLRequestDeserializer(configureDeserializerSettings ?? (_ => { })), ServiceLifetime.Singleton);
            builder.Services.Configure<JsonSerializerOptions>(opt =>
            {
                if (!opt.Converters.Any(y => y.GetType() == typeof(OperationMessageConverter)))
                {
                    opt.Converters.Add(new OperationMessageConverter());
                }
            });

            return SystemTextJson.GraphQLBuilderExtensions.AddSystemTextJson(builder, configureSerializerSettings);
        }
    }
}
