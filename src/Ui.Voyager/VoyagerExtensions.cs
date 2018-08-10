using System;
using GraphQL.Server.Ui.Voyager;
using Microsoft.AspNetCore.Builder;

namespace GraphQL.Server
{
    public static class VoyagerExtensions
    {
        public static IApplicationBuilder UseGraphQLVoyager(this IApplicationBuilder app, GraphQLVoyagerOptions options)
        {
            if (options == null)
                options = new GraphQLVoyagerOptions();

            app.UseMiddleware<VoyagerMiddleware>(options);
            return app;
        }

        public static IApplicationBuilder UseGraphQLVoyager(this IApplicationBuilder app)
        {
            return app.UseGraphQLVoyager(new GraphQLVoyagerOptions());
        }

        public static IApplicationBuilder UseGraphQLVoyager(this IApplicationBuilder app, Action<GraphQLVoyagerOptions> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            var options = new GraphQLVoyagerOptions();
            configure(options);

            return app.UseGraphQLVoyager(options);
        }
    }
}
