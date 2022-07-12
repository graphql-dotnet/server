using GraphQL.Server.Ui.Playground;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extensions for <see cref="IApplicationBuilder"/> to add <see cref="PlaygroundMiddleware"/> in the HTTP request pipeline.
/// </summary>
public static class PlaygroundApplicationBuilderExtensions
{
    /// <summary> Adds middleware for GraphQL Playground using the specified options. </summary>
    /// <param name="app"> <see cref="IApplicationBuilder"/> to configure an application's request pipeline. </param>
    /// <param name="options"> Options to customize <see cref="PlaygroundMiddleware"/>. If not set, then the default values will be used. </param>
    /// <param name="path">The path to the GraphQL Playground endpoint which defaults to '/ui/playground'</param>
    /// <returns> The reference to provided <paramref name="app"/> instance. </returns>
    public static IApplicationBuilder UseGraphQLPlayground(this IApplicationBuilder app, string path = "/ui/playground", PlaygroundOptions? options = null)
    {
        return app.UseWhen(
            context => HttpMethods.IsGet(context.Request.Method) && !context.WebSockets.IsWebSocketRequest &&
                context.Request.Path.StartsWithSegments(path, out var remaining) && string.IsNullOrEmpty(remaining),
            b => b.UseMiddleware<PlaygroundMiddleware>(options ?? new PlaygroundOptions()));
    }
}
