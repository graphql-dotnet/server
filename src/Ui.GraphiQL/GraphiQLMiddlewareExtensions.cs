using GraphQL.Server.Ui.GraphiQL;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for <see cref="GraphiQLMiddleware"/>
    /// </summary>
    public static class GraphiQLMiddlewareExtensions
    {
        /// <summary>
        /// Enables a GraphiQLServer using the specified settings
        /// </summary>
        /// <param name="applicationBuilder"> <see cref="IApplicationBuilder"/> to configure an application's request pipeline. </param>
        /// <param name="settings">Options to customize <see cref="GraphiQLMiddleware"/>.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseGraphiQLServer(this IApplicationBuilder applicationBuilder, GraphiQLOptions settings = null)
        {
            return applicationBuilder.UseMiddleware<GraphiQLMiddleware>(settings ?? new GraphiQLOptions());
        }
    }
}
