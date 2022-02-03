using System;
using GraphQL.DI;
using GraphQL.Server.Transports.AspNetCore;
using GraphQL.Server.Transports.AspNetCore.NewtonsoftJson;
using Newtonsoft.Json;

namespace GraphQL.Server
{
    public static class GraphQLBuilderNewtonsoftJsonExtensions
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
        public static IGraphQLBuilder AddNewtonsoftJson(this IGraphQLBuilder builder,
            Action<JsonSerializerSettings> configureDeserializerSettings = null,
            Action<JsonSerializerSettings> configureSerializerSettings = null)
        {
            builder.Services.Register<IGraphQLRequestDeserializer>(p => new GraphQLRequestDeserializer(configureDeserializerSettings ?? (_ => { })), ServiceLifetime.Singleton);
            return NewtonsoftJson.GraphQLBuilderExtensions.AddNewtonsoftJson(builder, configureSerializerSettings);
        }
    }
}
