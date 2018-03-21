using GraphQL.Samples.Schemas.Chat;
using GraphQL.Server.Transports.AspNetCore;
using GraphQL.Server.Transports.Subscriptions.Abstractions;
using GraphQL.Server.Transports.WebSockets;
using GraphQL.Server.Ui.GraphiQL;
using GraphQL.Server.Ui.Playground;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MessageType = GraphQL.Samples.Schemas.Chat.MessageType;

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

            // http
            services.AddGraphQLHttp();

            // subscriptions
            services.Configure<ExecutionOptions<ChatSchema>>(options =>
            {
                options.EnableMetrics = true;
                options.ExposeExceptions = true;
            });
            
            // this will be called before the protocol listener
            services.AddSingleton<IOperationMessageListener, LogMessagesListener>();

            // register default services for web socket. This will also add the protocol handler.
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
            app.UseGraphQLPlayground(new GraphQLPlaygroundOptions()
            {
                Path = "/ui/playground"
            });
            app.UseGraphiQLServer(new GraphiQLOptions
            {
                GraphiQLPath = "/ui/graphiql",
                GraphQLEndPoint = "/graphql"
            });
            app.UseMvc();
        }
    }
}
