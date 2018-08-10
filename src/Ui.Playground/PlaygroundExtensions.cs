using System;
using GraphQL.Server.Ui.Playground;
using Microsoft.AspNetCore.Builder;

namespace GraphQL.Server
{
    public static class PlaygroundExtensions
    {
        public static IApplicationBuilder UseGraphQLPlayground(this IApplicationBuilder app, GraphQLPlaygroundOptions options)
        {
            if (options == null)
                options = new GraphQLPlaygroundOptions();

            app.UseMiddleware<PlaygroundMiddleware>(options);
            return app;
        }

        public static IApplicationBuilder UseGraphQLPlayground(this IApplicationBuilder app)
        {
            return app.UseGraphQLPlayground(new GraphQLPlaygroundOptions());
        }

        public static IApplicationBuilder UseGraphQLPlayground(this IApplicationBuilder app, Action<GraphQLPlaygroundOptions> configure)
        {
            var options = new GraphQLPlaygroundOptions();
            configure(options);

            return app.UseGraphQLPlayground(options);
        }
    }
}
