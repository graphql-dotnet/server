using GraphQL.Server.Transports.AspNetCore;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extensions for <see cref="IApplicationBuilder"/> to add <see cref="GraphQLHttpMiddleware{TSchema}"/>
    /// or its descendants in the HTTP request pipeline.
    /// </summary>
    public static class GraphQLHttpApplicationBuilderExtensions
    {
        /// <summary>
        /// Add the GraphQL middleware to the HTTP request pipeline
        /// </summary>
        /// <typeparam name="TSchema">The implementation of <see cref="ISchema"/> to use</typeparam>
        /// <param name="builder">The application builder</param>
        /// <param name="path">The path to the GraphQL endpoint which defaults to '/graphql'</param>
        /// <returns>The <see cref="IApplicationBuilder"/> received as parameter</returns>
        public static IApplicationBuilder UseGraphQL<TSchema>(this IApplicationBuilder builder, string path = "/graphql")
            where TSchema : ISchema
            => builder.UseGraphQL<TSchema>(new PathString(path));

        /// <summary>
        /// Add the GraphQL middleware to the HTTP request pipeline
        /// </summary>
        /// <typeparam name="TSchema">The implementation of <see cref="ISchema"/> to use</typeparam>
        /// <param name="builder">The application builder</param>
        /// <param name="path">The path to the GraphQL endpoint</param>
        /// <returns>The <see cref="IApplicationBuilder"/> received as parameter</returns>
        public static IApplicationBuilder UseGraphQL<TSchema>(this IApplicationBuilder builder, PathString path)
            where TSchema : ISchema
        {
            return builder.UseWhen(
                context => context.Request.Path.StartsWithSegments(path, out var remaining) && string.IsNullOrEmpty(remaining),
                b => b.UseMiddleware<GraphQLHttpMiddleware<TSchema>>());
        }

        /// <summary>
        /// Add the GraphQL custom middleware to the HTTP request pipeline
        /// </summary>
        /// <typeparam name="TSchema">The implementation of <see cref="ISchema"/> to use</typeparam>
        /// <typeparam name="TMiddleware">Custom middleware inherited from <see cref="GraphQLHttpMiddleware{TSchema}"/></typeparam>
        /// <param name="builder">The application builder</param>
        /// <param name="path">The path to the GraphQL endpoint which defaults to '/graphql'</param>
        /// <returns>The <see cref="IApplicationBuilder"/> received as parameter</returns>
        public static IApplicationBuilder UseGraphQL<TSchema, TMiddleware>(this IApplicationBuilder builder, string path = "/graphql")
            where TSchema : ISchema
            where TMiddleware : GraphQLHttpMiddleware<TSchema>
            => builder.UseGraphQL<TSchema, TMiddleware>(new PathString(path));

        /// <summary>
        /// Add the GraphQL custom middleware to the HTTP request pipeline
        /// </summary>
        /// <typeparam name="TSchema">The implementation of <see cref="ISchema"/> to use</typeparam>
        /// <typeparam name="TMiddleware">Custom middleware inherited from <see cref="GraphQLHttpMiddleware{TSchema}"/></typeparam>
        /// <param name="builder">The application builder</param>
        /// <param name="path">The path to the GraphQL endpoint</param>
        /// <returns>The <see cref="IApplicationBuilder"/> received as parameter</returns>
        public static IApplicationBuilder UseGraphQL<TSchema, TMiddleware>(this IApplicationBuilder builder, PathString path)
            where TSchema : ISchema
            where TMiddleware : GraphQLHttpMiddleware<TSchema>
        {
            return builder.UseWhen(
                context => context.Request.Path.StartsWithSegments(path, out var remaining) && string.IsNullOrEmpty(remaining),
                b => b.UseMiddleware<TMiddleware>());
        }
    }
}
