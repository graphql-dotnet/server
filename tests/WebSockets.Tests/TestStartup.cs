using GraphQL.Transports.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server.Transports.WebSockets.Tests
{
    public class TestStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGraphQL();
            services.AddGraphQLWebSocketsTransport<TestSchema>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseWebSockets();
            app.UseGraphQLEndPoint<TestSchema>("/graphql");
        }
    }
}