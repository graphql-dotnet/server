using GraphQL.Server.Ui.Playground;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for <see cref="IApplicationBuilder"/>
    /// </summary>
    public static class PlaygroundExtensions
    {
        /// <summary> Adds middleware for GraphQL Playground using the specified options. </summary>
        /// <param name="app"> <see cref="IApplicationBuilder"/> to configure an application's request pipeline. </param>
        /// <param name="options"> Options to customize <see cref="PlaygroundMiddleware"/>. If not set, then the default values will be used. </param>
        /// <returns> The reference to provided <paramref name="app"/> instance. </returns>
        public static IApplicationBuilder UseGraphQLPlayground(this IApplicationBuilder app, GraphQLPlaygroundOptions options = null)
        {
            return app.UseMiddleware<PlaygroundMiddleware>(options ?? new GraphQLPlaygroundOptions());
        }
    }
}
