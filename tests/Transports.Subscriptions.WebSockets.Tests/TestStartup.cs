using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server.Transports.WebSockets.Tests
{
    public class TestStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<TestSchema>();
            services.AddGraphQL()
                .AddWebSockets();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseWebSockets();
            app.UseGraphQLWebSockets<TestSchema>("/graphql");
        }
    }
}