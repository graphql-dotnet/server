using Microsoft.AspNetCore.Builder;

namespace GraphQL.Server.Ui.Voyager
{
    /// <summary>
    /// Extension methods for <see cref="VoyagerMiddleware"/>
    /// </summary>
    public static class VoyagerExtensions
    {
        /// <summary> Adds middleware for GraphQL Voyager. </summary>
        /// <param name="app"> <see cref="IApplicationBuilder"/> to configure an application's request pipeline. </param>
        /// <param name="options"> Options to customize <see cref="VoyagerMiddleware"/>. </param>
        /// <returns> The <see cref="IApplicationBuilder"/> instance. </returns>
        public static IApplicationBuilder UseGraphQLVoyager(this IApplicationBuilder app, GraphQLVoyagerOptions options = null)
        {
            return app.UseMiddleware<VoyagerMiddleware>(options ?? new GraphQLVoyagerOptions());
        }
    }
}
