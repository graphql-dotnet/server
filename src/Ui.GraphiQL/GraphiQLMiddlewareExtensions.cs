using GraphQL.Server.Ui.GraphiQL;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for <see cref="IApplicationBuilder"/>
    /// </summary>
    public static class GraphiQLMiddlewareExtensions
    {
        /// <summary> Adds middleware for GraphiQL using the specified options. </summary>
        /// <param name="app"> <see cref="IApplicationBuilder"/> to configure an application's request pipeline. </param>
        /// <param name="options"> Options to customize <see cref="GraphiQLMiddleware"/>. If not set, then the default values will be used. </param>
        /// <returns> The reference to provided <paramref name="app"/> instance. </returns>
        public static IApplicationBuilder UseGraphiQLServer(this IApplicationBuilder app, GraphiQLOptions options = null)
            => app.UseMiddleware<GraphiQLMiddleware>(options ?? new GraphiQLOptions());
    }
}
