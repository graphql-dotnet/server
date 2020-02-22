using GraphQL.Server.Ui.Voyager;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for <see cref="IApplicationBuilder"/>
    /// </summary>
    public static class VoyagerExtensions
    {
        /// <summary> Adds middleware for GraphQL Voyager using the specified options. </summary>
        /// <param name="app"> <see cref="IApplicationBuilder"/> to configure an application's request pipeline. </param>
        /// <param name="options"> Options to customize <see cref="VoyagerMiddleware"/>. If not set, then the default values will be used. </param>
        /// <returns> The reference to provided <paramref name="app"/> instance. </returns>
        public static IApplicationBuilder UseGraphQLVoyager(this IApplicationBuilder app, GraphQLVoyagerOptions options = null)
            => app.UseMiddleware<VoyagerMiddleware>(options ?? new GraphQLVoyagerOptions());
    }
}
