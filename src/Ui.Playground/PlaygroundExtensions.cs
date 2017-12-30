using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace GraphQL.Server.Ui.Playground
{
    public static class PlaygroundExtensions
    {
        public static IApplicationBuilder UseGraphQLPlayground(this IApplicationBuilder app, GraphQLPlaygroundOptions options)
        {
            app.UseFileServer(new FileServerOptions()
            {
                RequestPath = new PathString(options.Path),
                FileProvider = new EmbeddedFileProvider(
                    typeof(PlaygroundExtensions).GetTypeInfo().Assembly,
                    "GraphQL.Server.Ui.Playground.Html")
            });

            return app;
        }
    }
}
