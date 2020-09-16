using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GraphQL.Server.Transports.WebSockets.Tests
{
    public class TestStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<TestSchema>();
            services.AddGraphQL()
                .AddWebSockets();
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddDebug();
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseWebSockets();
            app.UseGraphQLWebSockets<TestSchema>("/graphql");
        }
    }
}
