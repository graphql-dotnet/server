using GraphQL.Server.Tests.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server.Transports.AspNetCore.Tests
{
    public class TestStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<TestSchema>();
            services.AddGraphQL();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseGraphQL<TestSchema>();
        }
    }
}
