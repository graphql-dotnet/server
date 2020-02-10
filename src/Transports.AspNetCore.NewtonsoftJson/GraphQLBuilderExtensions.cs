using GraphQL.NewtonsoftJson;
using GraphQL.Server.Transports.AspNetCore.Common;
using GraphQL.Server.Transports.AspNetCore.NewtonsoftJson;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;

namespace GraphQL.Server
{
    public static class GraphQLBuilderExtensions
    {
        /// <summary>
        /// Adds a <see cref="IGraphQLRequestDeserializer"/> and a <see cref="IDocumentWriter"/>
        /// to the service collection with the provided configuration/settings.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configureDeserializerSettings">
        /// Action to further configure the request deserializer's settings.
        /// Affects reading of the JSON from the HTTP request the middleware processes.
        /// </param>
        /// <param name="serializerSettings">
        /// Settings for the response serializer.
        /// Affects JSON returned by the middleware.
        /// </param>
        /// <returns>GraphQL Builder.</returns>
        public static IGraphQLBuilder AddNewtonsoftJson(this IGraphQLBuilder builder,
            Action<JsonSerializerSettings> configureDeserializerSettings = null,
            JsonSerializerSettings serializerSettings = null)
        {
            builder.Services.AddSingleton<IGraphQLRequestDeserializer>(p => new GraphQLRequestDeserializer(configureDeserializerSettings));
            builder.Services.AddSingleton<IDocumentWriter>(p => serializerSettings == null ? new DocumentWriter() : new DocumentWriter(serializerSettings));

            return builder;
        }
    }
}
