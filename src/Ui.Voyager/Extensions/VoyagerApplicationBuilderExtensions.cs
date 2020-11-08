using GraphQL.Server.Ui.Voyager;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extensions for <see cref="IApplicationBuilder"/> to add <see cref="VoyagerMiddleware"/> in the HTTP request pipeline.
    /// </summary>
    public static class VoyagerApplicationBuilderExtensions
    {
        /// <summary> Adds middleware for Voyager using default options. </summary>
        /// <param name="app"> <see cref="IApplicationBuilder"/> to configure an application's request pipeline. </param>
        /// <param name="path">The path to the Voyager endpoint which defaults to '/ui/voyager'</param>
        /// <returns> The reference to provided <paramref name="app"/> instance. </returns>
        public static IApplicationBuilder UseGraphQLVoyager(this IApplicationBuilder app, string path = "/ui/voyager")
            => app.UseGraphQLVoyager(new VoyagerOptions(), path);

        /// <summary> Adds middleware for Voyager using the specified options. </summary>
        /// <param name="app"> <see cref="IApplicationBuilder"/> to configure an application's request pipeline. </param>
        /// <param name="options"> Options to customize <see cref="VoyagerMiddleware"/>. If not set, then the default values will be used. </param>
        /// <param name="path">The path to the Voyager endpoint which defaults to '/ui/voyager'</param>
        /// <returns> The reference to provided <paramref name="app"/> instance. </returns>
        public static IApplicationBuilder UseGraphQLVoyager(this IApplicationBuilder app, VoyagerOptions options, string path = "/ui/voyager")
        {
            return app.UseWhen(
               context => context.Request.Path.StartsWithSegments(path, out var remaining) && string.IsNullOrEmpty(remaining),
               b => b.UseMiddleware<VoyagerMiddleware>(options ?? new VoyagerOptions()));
        }
    }
}
