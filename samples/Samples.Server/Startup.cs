using GraphQL.Samples.Schemas.Chat;
using GraphQL.Server;
using GraphQL.Server.Ui.GraphiQL;
using GraphQL.Server.Ui.Playground;
using GraphQL.Server.Ui.Voyager;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

#if !NETCOREAPP2_2
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;
#endif

namespace GraphQL.Samples.Server
{
    public class Startup
    {
#if NETCOREAPP2_2
        public Startup(IConfiguration configuration, IHostingEnvironment environment)
#else
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
#endif
        {
            Configuration = configuration;
            Environment = environment;
        }

        public IConfiguration Configuration { get; }

#if NETCOREAPP2_2
        public IHostingEnvironment Environment { get; }
#else
        public IWebHostEnvironment Environment { get; }
#endif

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
#if NETCOREAPP3_0
            // Workaround until GraphQL can swap off Newtonsoft.Json and onto the new MS one.
            // Depending on whether you're using IIS or Kestrel, the code required is different
            // See: https://github.com/graphql-dotnet/graphql-dotnet/issues/1116
            services.Configure<KestrelServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });
            services.Configure<IISServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });
#endif

            services
                .AddSingleton<IChat, Chat>()
                .AddSingleton<ChatSchema>()
                .AddGraphQL(options =>
                {
                    options.EnableMetrics = true;
                    options.ExposeExceptions = Environment.IsDevelopment();
                    options.UnhandledExceptionDelegate = ctx =>
                    {
                        Console.WriteLine("error: " + ctx.OriginalException.Message);
                    };
                })
                .AddWebSockets()
                .AddDataLoader()
                .AddGraphTypes(typeof(ChatSchema));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseWebSockets();
            app.UseGraphQLWebSockets<ChatSchema>("/graphql");

            app.UseGraphQL<ChatSchema, GraphQLHttpMiddlewareWithLogs<ChatSchema>>("/graphql");
            
            app.UseGraphQLPlayground(new GraphQLPlaygroundOptions
            {
                Path = "/ui/playground",
                PlaygroundSettings = new Dictionary<string, object>
                {
                    ["editor.theme"] = "light",
                    ["tracing.hideTracingResponse"] = false,
                }
            });

            app.UseGraphiQLServer(new GraphiQLOptions
            {
                Path = "/ui/graphiql",
                GraphQLEndPoint = "/graphql",
            });

            app.UseGraphQLVoyager(new GraphQLVoyagerOptions
            {
                Path = "/ui/voyager",
                GraphQLEndPoint = "/graphql",
            });
        }
    }
}
