using GraphQL.Server.Ui.SmartPlayground;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder
{

    /// <summary>
    /// Extensions for <see cref="IApplicationBuilder"/> to add <see cref="SmartPlaygroundMiddleware"/> in the HTTP request pipeline.
    /// </summary>
    public static class SmartPlaygroundApplicationBuilderExtensions
    {
        /// <summary> Adds middleware for GraphQL Playground using the specified options. </summary>
        /// <param name="app"> <see cref="IApplicationBuilder"/> to configure an application's request pipeline. </param>
        /// <param name="options"> Options to customize <see cref="SmartPlaygroundMiddleware"/>. If not set, then the default values will be used. </param>
        /// <returns> The reference to provided <paramref name="app"/> instance. </returns>
        public static IApplicationBuilder UseSmartGraphQLPlayground(this IApplicationBuilder app)
        {
            return app
                .UseWhen(
                   context => HttpMethods.IsGet(context.Request.Method) && context.Request.Path.StartsWithSegments("/" + Constants.SmartPlaygroundPath, out var remaining),
                   b => b.UseMiddleware<SmartPlaygroundMiddleware>());
        }
    }
}
