using GraphQL.Server.Tests.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server.Transports.AspNetCore.Tests
{
    public class AuthenticatedTestStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(auth =>
            {
                auth.DefaultScheme = TestAuthenticationDefaults.AuthenticationScheme;
            })
            .AddTestAuthentication(options =>
            {
                options.RoleClaimType = "role";
            });

            services.AddAuthorization(auth =>
            {
                auth.AddPolicy("IsAdmin", policy => policy
                    .RequireAuthenticatedUser()
                    .RequireRole("admin")
                );
            });

            services.AddSingleton<TestSchema>();

            services.AddGraphQL()
                .AddHttpAuthorization();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseAuthentication();

            app.UseGraphQL<TestSchema>(options =>
            {
                options.AuthorizationPolicyName = "IsAdmin";
            });
        }
    }
}
