using GraphQL.Server.Transports.AspNetCore;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder
{
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Use the GraphQLHttp middleware with the provided options
        /// </summary>
        /// <typeparam name="TSchema">The implementation of <see cref="ISchema"/> to use</typeparam>
        /// <returns>The <see cref="IApplicationBuilder"/> received as parameter</returns>
        public static IApplicationBuilder UseGraphQL<TSchema>(this IApplicationBuilder builder,
            PathString path)
            where TSchema : ISchema
        {
            return builder.Map(path, x => x.UseMiddleware<GraphQLHttpMiddleware<TSchema>>());
        }
    }
}
