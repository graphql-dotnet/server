using System;
using GraphQL.Server.Transports.WebSockets;
using GraphQL.Types;
using Microsoft.AspNetCore.Builder;

namespace GraphQL.Server
{
    public static class GraphQLWebSocketsExtensions
    {
        /// <summary>
        /// Add GraphQL web sockets middleware to the request pipeline
        /// </summary>
        /// <typeparam name="TSchema">The implementation of <see cref="ISchema"/> to use</typeparam>
        /// <param name="builder"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseGraphQLWebSockets<TSchema>(
            this IApplicationBuilder builder,
            GraphQLWebSocketsMiddlewareOptions options)
            where TSchema : ISchema
        {
            return builder.UseMiddleware<GraphQLWebSocketsMiddleware<TSchema>>(options ?? new GraphQLWebSocketsMiddlewareOptions());
        }

        /// <summary>
        /// Add GraphQL web sockets middleware to the request pipeline
        /// </summary>
        /// <typeparam name="TSchema">The implementation of <see cref="ISchema"/> to use</typeparam>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseGraphQLWebSockets<TSchema>(this IApplicationBuilder builder)
            where TSchema : ISchema
        {
            return builder.UseGraphQLWebSockets<TSchema>(new GraphQLWebSocketsMiddlewareOptions());
        }

        /// <summary>
        /// Add GraphQL web sockets middleware to the request pipeline
        /// </summary>
        /// <typeparam name="TSchema">The implementation of <see cref="ISchema"/> to use</typeparam>
        /// <param name="builder"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseGraphQLWebSockets<TSchema>(this IApplicationBuilder builder,
            Action<GraphQLWebSocketsMiddlewareOptions> configure)
            where TSchema : ISchema
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            var options = new GraphQLWebSocketsMiddlewareOptions();
            configure(options);

            return builder.UseGraphQLWebSockets<TSchema>(options);
        }
    }
}