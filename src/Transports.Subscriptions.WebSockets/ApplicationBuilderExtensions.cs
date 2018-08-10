using GraphQL.Server.Transports.WebSockets;
using GraphQL.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server
{
    public static class GraphQLWebSocketsExtensions
    {
        /// <summary>
        /// Add GraphQL web sockets middleware to the request pipeline
        /// </summary>
        /// <typeparam name="TSchema"></typeparam>
        /// <param name="builder"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseGraphQLWebSockets<TSchema>(this IApplicationBuilder builder,
            PathString path)
            where TSchema : ISchema
        {
            return builder.Map(path, x => x.UseMiddleware<GraphQLWebSocketsMiddleware<TSchema>>());
        }
    }
}