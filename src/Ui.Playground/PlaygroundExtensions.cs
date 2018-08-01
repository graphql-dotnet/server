using System;
using Microsoft.AspNetCore.Builder;

namespace GraphQL.Server.Ui.Playground {

    public static class PlaygroundExtensions {

        public static IApplicationBuilder UseGraphQLPlayground(this IApplicationBuilder app, GraphQLPlaygroundOptions options)
        {
            if (options == null)
                options = new GraphQLPlaygroundOptions();

            app.UseMiddleware<PlaygroundMiddleware>(options);
            return app;
        }

    }

}
