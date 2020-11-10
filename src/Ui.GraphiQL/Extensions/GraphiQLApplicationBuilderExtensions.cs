using GraphQL.Server.Ui.GraphiQL;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extensions for <see cref="IApplicationBuilder"/> to add <see cref="GraphiQLMiddleware"/> in the HTTP request pipeline.
    /// </summary>
    public static class GraphiQLApplicationBuilderExtensions
    {
        /// <summary> Adds middleware for GraphiQL using default options. </summary>
        /// <param name="app"> <see cref="IApplicationBuilder"/> to configure an application's request pipeline. </param>
        /// <param name="path">The path to the GraphiQL endpoint which defaults to '/ui/graphiql'</param>
        /// <returns> The reference to provided <paramref name="app"/> instance. </returns>
        public static IApplicationBuilder UseGraphQLGraphiQL(this IApplicationBuilder app, string path = "/ui/graphiql")
            => app.UseGraphQLGraphiQL(new GraphiQLOptions(), path);

        /// <summary> Adds middleware for GraphiQL using the specified options. </summary>
        /// <param name="app"> <see cref="IApplicationBuilder"/> to configure an application's request pipeline. </param>
        /// <param name="options"> Options to customize <see cref="GraphiQLMiddleware"/>. If not set, then the default values will be used. </param>
        /// <param name="path">The path to the GraphiQL endpoint which defaults to '/ui/graphiql'</param>
        /// <returns> The reference to provided <paramref name="app"/> instance. </returns>
        public static IApplicationBuilder UseGraphQLGraphiQL(this IApplicationBuilder app, GraphiQLOptions options, string path = "/ui/graphiql")
        {
            return app.UseWhen(
               context => context.Request.Path.StartsWithSegments(path, out var remaining) && string.IsNullOrEmpty(remaining),
               b => b.UseMiddleware<GraphiQLMiddleware>(options ?? new GraphiQLOptions()));
        }
    }
}
