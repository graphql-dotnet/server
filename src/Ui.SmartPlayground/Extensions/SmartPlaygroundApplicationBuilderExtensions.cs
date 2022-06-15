using GraphQL.Server.Ui.SmartPlayground;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder
{

    /// <summary>
    /// Extensions for <see cref="IApplicationBuilder"/> to add <see cref="PlaygroundMiddleware"/> in the HTTP request pipeline.
    /// </summary>
    public static class SmartPlaygroundApplicationBuilderExtensions
    {
        /// <summary> Adds middleware for GraphQL Playground using default options. </summary>
        /// <param name="app"> <see cref="IApplicationBuilder"/> to configure an application's request pipeline. </param>
        /// <param name="authorizeUrl">The URI to the OAuth2 authorize endpoint</param>
        /// <param name="tokenUrl">The URI to the OAuth2 token endpoint</param>
        /// <param name="path">The path to the GraphQL Playground endpoint which defaults to '/ui/playground'</param>
        /// <returns> The reference to provided <paramref name="app"/> instance. </returns>
        public static IApplicationBuilder UseSmartGraphQLPlayground(this IApplicationBuilder app, string authorizeUrl, string tokenUrl, string path = "/ui/smartplayground")
            => app.UseSmartGraphQLPlayground(
                new SmartPlaygroundOptions
                {
                    AuthorizeUrl = new Uri(authorizeUrl),
                    TokenUrl = new Uri(tokenUrl)
                },
                path);

        /// <summary> Adds middleware for GraphQL Playground using the specified options. </summary>
        /// <param name="app"> <see cref="IApplicationBuilder"/> to configure an application's request pipeline. </param>
        /// <param name="options"> Options to customize <see cref="PlaygroundMiddleware"/>. If not set, then the default values will be used. </param>
        /// <param name="path">The path to the GraphQL Playground endpoint which defaults to '/ui/smartplayground'</param>
        /// <returns> The reference to provided <paramref name="app"/> instance. </returns>
        public static IApplicationBuilder UseSmartGraphQLPlayground(this IApplicationBuilder app, SmartPlaygroundOptions options, string path = "/ui/smartplayground")
        {
            return app
                .UseWhen(
                   context => HttpMethods.IsGet(context.Request.Method) && context.Request.Path.StartsWithSegments(path, out var remaining),
                   b => b.UseMiddleware<SmartPlaygroundMiddleware>(options ?? new SmartPlaygroundOptions()));
        }
    }
}
