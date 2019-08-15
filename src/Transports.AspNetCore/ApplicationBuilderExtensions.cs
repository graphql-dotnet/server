using GraphQL.Server.Transports.AspNetCore;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extensions for <see cref="IApplicationBuilder"/> to add <see cref="GraphQLHttpMiddleware{TSchema}"/> in the HTTP request pipeline.
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Add the GraphQL middleware to the HTTP request pipeline
        /// </summary>
        /// <typeparam name="TSchema">The implementation of <see cref="ISchema"/> to use</typeparam>
        /// <param name="builder">The application builder.</param>
        /// <param name="path">The path to the GraphQL endpoint which defaults to '/graphql'</param>
        /// <param name="configure">Action to configure json serialization settings</param>
        /// <returns>The <see cref="IApplicationBuilder"/> received as parameter</returns>
        public static IApplicationBuilder UseGraphQL<TSchema>(this IApplicationBuilder builder, string path = "/graphql", Action<JsonSerializerSettings> configure = null)
            where TSchema : ISchema
        {
            return builder.UseGraphQL<TSchema>(new PathString(path), configure);
        }

        /// <summary>
        /// Add the GraphQL middleware to the HTTP request pipeline
        /// </summary>
        /// <typeparam name="TSchema">The implementation of <see cref="ISchema"/> to use</typeparam>
        /// <param name="builder">The application builder.</param>
        /// <param name="path">The path to the GraphQL endpoint</param>
        /// <param name="configure">Action to configure json serialization settings</param>
        /// <returns>The <see cref="IApplicationBuilder"/> received as parameter</returns>
        public static IApplicationBuilder UseGraphQL<TSchema>(this IApplicationBuilder builder, PathString path, Action<JsonSerializerSettings> configure = null)
            where TSchema : ISchema
        {
            return builder.UseMiddleware<GraphQLHttpMiddleware<TSchema>>(path, configure ?? (_ => { }));
        }
    }
}
