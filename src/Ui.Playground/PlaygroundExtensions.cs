using Microsoft.AspNetCore.Builder;

namespace GraphQL.Server.Ui.Playground {

    public static class PlaygroundExtensions {

        public static IApplicationBuilder UseGraphQLPlayground(this IApplicationBuilder app, GraphQLPlaygroundOptions options = null)
        {
            return app.UseMiddleware<PlaygroundMiddleware>(options ?? new GraphQLPlaygroundOptions());
        }
    }
}
