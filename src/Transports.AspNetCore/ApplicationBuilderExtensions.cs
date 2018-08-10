using System;
using GraphQL.Server.Transports.AspNetCore;
using GraphQL.Types;
using Microsoft.AspNetCore.Builder;

namespace GraphQL.Server
{
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Add the GraphQL middleware to the request pipeline
        /// </summary>
        /// <typeparam name="TSchema">The implementation of <see cref="ISchema"/> to use</typeparam>
        /// <param name="builder">The application builder.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> received as parameter</returns>
        public static IApplicationBuilder UseGraphQL<TSchema>(this IApplicationBuilder builder)
            where TSchema : ISchema
        {
            return builder.UseGraphQL<TSchema>(new GraphQLHttpMiddlewareOptions());
        }

        /// <summary>
        /// Add the GraphQL middleware to the request pipeline
        /// </summary>
        /// <typeparam name="TSchema">The implementation of <see cref="ISchema"/> to use</typeparam>
        /// <param name="builder">The application builder.</param>
        /// <param name="options"></param>
        /// <returns>The <see cref="IApplicationBuilder"/> received as parameter</returns>
        public static IApplicationBuilder UseGraphQL<TSchema>(this IApplicationBuilder builder,
            GraphQLHttpMiddlewareOptions options)
            where TSchema : ISchema
        {
            return builder.UseMiddleware<GraphQLHttpMiddleware<TSchema>>(options ?? new GraphQLHttpMiddlewareOptions());
        }

        /// <summary>
        /// Add the GraphQL middleware to the request pipeline
        /// </summary>
        /// <typeparam name="TSchema">The implementation of <see cref="ISchema"/> to use</typeparam>
        /// <param name="builder">The application builder.</param>
        /// <param name="configure"></param>
        /// <returns>The <see cref="IApplicationBuilder"/> received as parameter</returns>
        public static IApplicationBuilder UseGraphQL<TSchema>(this IApplicationBuilder builder,
            Action<GraphQLHttpMiddlewareOptions> configure)
            where TSchema : ISchema
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            var options = new GraphQLHttpMiddlewareOptions();
            configure(options);

            return builder.UseGraphQL<TSchema>(options);
        }
    }
}
