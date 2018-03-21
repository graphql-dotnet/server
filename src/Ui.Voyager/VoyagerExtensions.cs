using System;
using Microsoft.AspNetCore.Builder;

namespace GraphQL.Server.Ui.Voyager
{
    public static class VoyagerExtensions
    {
        public static IApplicationBuilder UseGraphQLVoyager(this IApplicationBuilder app, GraphQLVoyagerOptions options)
        {
            if (options == null) { throw new ArgumentNullException(nameof(options)); }

            app.UseMiddleware<VoyagerMiddleware>(options);
            return app;
        }
    }
}
