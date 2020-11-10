using GraphQL.Server.Transports.WebSockets;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extensions for <see cref="IApplicationBuilder"/> to add <see cref="GraphQLWebSocketsMiddleware{TSchema}"/>
    /// or its descendants in the HTTP request pipeline.
    /// </summary>
    public static class GraphQLWebSocketsApplicationBuilderExtensions
    {
        /// <summary>
        /// Add GraphQL web sockets middleware to the request pipeline
        /// </summary>
        /// <typeparam name="TSchema">The implementation of <see cref="ISchema"/> to use</typeparam>
        /// <param name="builder">The application builder</param>
        /// <param name="path">The path to the GraphQL web socket endpoint which defaults to '/graphql'</param>
        /// <returns>The <see cref="IApplicationBuilder"/> received as parameter</returns>
        public static IApplicationBuilder UseGraphQLWebSockets<TSchema>(
            this IApplicationBuilder builder,
            string path = "/graphql")
            where TSchema : ISchema
            => builder.UseGraphQLWebSockets<TSchema>(new PathString(path));

        /// <summary>
        /// Add GraphQL web sockets middleware to the request pipeline
        /// </summary>
        /// <typeparam name="TSchema">The implementation of <see cref="ISchema"/> to use</typeparam>
        /// <param name="builder">The application builder</param>
        /// <param name="path">The path to the GraphQL endpoint</param>
        /// <returns>The <see cref="IApplicationBuilder"/> received as parameter</returns>
        public static IApplicationBuilder UseGraphQLWebSockets<TSchema>(
            this IApplicationBuilder builder,
            PathString path)
            where TSchema : ISchema
        {
            return builder.UseWhen(
                context => context.Request.Path.StartsWithSegments(path, out var remaining) && string.IsNullOrEmpty(remaining),
                b => b.UseMiddleware<GraphQLWebSocketsMiddleware<TSchema>>());
        }
    }
}
