using GraphQL.Server.Ui.Altair;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for <see cref="IApplicationBuilder"/>
    /// </summary>
    public static class AltairExtensions
    {
        /// <summary> Adds middleware for Altair GraphQL using the specified options. </summary>
        /// <param name="app"> <see cref="IApplicationBuilder"/> to configure an application's request pipeline. </param>
        /// <param name="options"> Options to customize <see cref="AltairMiddleware"/>. If not set, then the default values will be used. </param>
        /// <returns> The reference to provided <paramref name="app"/> instance. </returns>
        public static IApplicationBuilder UseGraphQLAltair(this IApplicationBuilder app, GraphQLAltairOptions options = null)
            => app.UseMiddleware<AltairMiddleware>(options ?? new GraphQLAltairOptions());
    }
}
