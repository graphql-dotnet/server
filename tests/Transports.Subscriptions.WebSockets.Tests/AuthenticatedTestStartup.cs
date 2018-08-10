using GraphQL.Server.Tests.Common;
using Microsoft.AspNetCore.Builder; 
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server.Transports.WebSockets.Tests
{
    public class AuthenticatedTestStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(auth =>
            {
                auth.DefaultScheme = TestAuthenticationDefaults.AuthenticationScheme;
            })
            .AddTestAuthentication();

            services.AddAuthorization(auth =>
            {
                auth.AddPolicy("IsAuthenticated", policy => policy
                    .RequireAuthenticatedUser());
            });

            services.AddSingleton<TestSchema>();

            services.AddGraphQL()
                .AddHttpAuthorization()
                .AddWebSockets();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseAuthentication();

            app.UseWebSockets();

            app.UseGraphQLWebSockets<TestSchema>(options =>
            {
                options.AuthorizationPolicyName = "IsAuthenticated";
            });
        }
    }
}