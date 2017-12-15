using GraphQL.Samples.Schemas.Chat;
using GraphQL.Server.Transports.AspNetCore;
using GraphQL.Server.Transports.WebSockets;
using GraphQL.Server.Transports.WebSockets.Events;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Samples.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IChat, Chat>();
            services.AddSingleton<ChatSchema>();
            services.AddSingleton<ChatQuery>();
            services.AddSingleton<ChatMutation>();
            services.AddSingleton<ChatSubscriptions>();
            services.AddSingleton<MessageType>();
            services.AddSingleton<MessageInputType>();

            // subscriptions
            services.AddSingleton<IEventAggregator, SimpleEventAggregator>();

            services.AddGraphQLHttp();
            services.AddGraphQLWebSocket<ChatSchema>();
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            
            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseWebSockets();
            app.UseGraphQLWebSocket<ChatSchema>(new GraphQLWebSocketsOptions());
            app.UseGraphQLHttp<ChatSchema>(new GraphQLHttpOptions());
            
            app.UseMvc();
        }
    }
}
