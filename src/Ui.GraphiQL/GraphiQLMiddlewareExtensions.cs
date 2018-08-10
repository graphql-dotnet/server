using System;
using GraphQL.Server.Ui.GraphiQL;
using Microsoft.AspNetCore.Builder;

namespace GraphQL.Server
{
    /// <summary>
    /// Extension methods for <see cref="GraphiQLMiddleware"/>
    /// </summary>
    public static class GraphiQLMiddlewareExtensions
    {
        /// <summary>
        /// Enables a GraphiQLServer using the specified options
        /// </summary>
        /// <param name="app"></param>
        /// <param name="options">The middleware options</param>
        /// <returns></returns>
        public static IApplicationBuilder UseGraphiQLServer(this IApplicationBuilder app, GraphiQLOptions options)
        {
            if (options == null)
                options = new GraphiQLOptions();

            app.UseMiddleware<GraphiQLMiddleware>(options);
            return app;
        }

        /// <summary>
        /// Enables a GraphiQLServer using the default options
        /// </summary>
        /// <param name="app"></param>
        /// <param name="options">The middleware options</param>
        /// <returns></returns>
        public static IApplicationBuilder UseGraphiQLServer(this IApplicationBuilder app)
        {
            return app.UseGraphiQLServer(new GraphiQLOptions());
        }

        /// <summary>
        /// Enables a GraphiQLServer
        /// </summary>
        /// <param name="app"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseGraphiQLServer(this IApplicationBuilder app, Action<GraphiQLOptions> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            var options = new GraphiQLOptions();
            configure(options);

            return app.UseGraphiQLServer(options);
        }
    }
}
