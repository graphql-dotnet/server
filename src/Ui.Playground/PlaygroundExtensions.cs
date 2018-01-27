using System;
using Microsoft.AspNetCore.Builder;

namespace GraphQL.Server.Ui.Playground {

    public static class PlaygroundExtensions {

        public static IApplicationBuilder UseGraphQLPlayground(this IApplicationBuilder app, GraphQLPlaygroundOptions options) {
            if (options == null) { throw new ArgumentNullException(nameof(options)); }

            app.UseMiddleware<PlaygroundMiddleware>(options);
            return app;
        }

    }

}
