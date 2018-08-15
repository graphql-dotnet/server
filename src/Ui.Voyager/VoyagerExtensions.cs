using Microsoft.AspNetCore.Builder;

namespace GraphQL.Server.Ui.Voyager
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
    }
}
