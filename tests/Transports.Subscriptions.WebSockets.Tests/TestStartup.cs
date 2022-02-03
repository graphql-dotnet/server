using GraphQL.MicrosoftDI;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GraphQL.Server.Transports.WebSockets.Tests
{
    public class TestStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddGraphQL(builder => builder.AddWebSockets())
                .AddSingleton<TestSchema>()
                .AddLogging(builder =>
                {
                    // prevent writing errors to Console.Error during tests (required for testing on ubuntu)
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
