using GraphQL.Server.Ui.Altair;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extensions for <see cref="IApplicationBuilder"/> to add <see cref="AltairMiddleware"/> in the HTTP request pipeline.
    /// </summary>
    public static class AltairApplicationBuilderExtensions
    {
        /// <summary> Adds middleware for Altair GraphQL UI using default options. </summary>
        /// <param name="app"> <see cref="IApplicationBuilder"/> to configure an application's request pipeline. </param>
        /// <param name="path">The path to the Altair GraphQL UI endpoint which defaults to '/ui/altair'</param>
        /// <returns> The reference to provided <paramref name="app"/> instance. </returns>
        public static IApplicationBuilder UseGraphQLAltair(this IApplicationBuilder app, string path = "/ui/altair")
            => app.UseGraphQLAltair(new AltairOptions(), path);

        /// <summary> Adds middleware for Altair GraphQL UI using the specified options. </summary>
        /// <param name="app"> <see cref="IApplicationBuilder"/> to configure an application's request pipeline. </param>
        /// <param name="options"> Options to customize <see cref="AltairMiddleware"/>. If not set, then the default values will be used. </param>
        /// <param name="path">The path to the Altair GraphQL UI endpoint which defaults to '/ui/altair'</param>
        /// <returns> The reference to provided <paramref name="app"/> instance. </returns>
        public static IApplicationBuilder UseGraphQLAltair(this IApplicationBuilder app, AltairOptions options, string path = "/ui/altair")
        {
            return app.UseWhen(
               context => context.Request.Path.StartsWithSegments(path, out var remaining) && string.IsNullOrEmpty(remaining),
               b => b.UseMiddleware<AltairMiddleware>(options ?? new AltairOptions()));
        }
    }
}
