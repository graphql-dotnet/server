using Microsoft.AspNetCore.Builder;

namespace GraphQL.Server.Ui.Playground
{
    /// <summary>
    /// Extension methods for <see cref="PlaygroundMiddleware"/>
    /// </summary>
    public static class PlaygroundExtensions
    {
        /// <summary> Adds middleware for GraphQL Playground. </summary>
        /// <param name="app"> <see cref="IApplicationBuilder"/> to configure an application's request pipeline. </param>
        /// <param name="options"> Options to customize <see cref="PlaygroundMiddleware"/>. </param>
        /// <returns> The <see cref="IApplicationBuilder"/> instance. </returns>
        public static IApplicationBuilder UseGraphQLPlayground(this IApplicationBuilder app, GraphQLPlaygroundOptions options = null)
        {
            return app.UseMiddleware<PlaygroundMiddleware>(options ?? new GraphQLPlaygroundOptions());
        }
    }
}
