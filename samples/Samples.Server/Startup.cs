using GraphQL.Samples.Schemas.Chat;
using GraphQL.Server;
using GraphQL.Server.Ui.GraphiQL;
using GraphQL.Server.Ui.Playground;
using GraphQL.Server.Ui.Altair;
using GraphQL.Server.Ui.Voyager;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

#if !NETCOREAPP2_2
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
            services
                .AddSingleton<IChat, Chat>()
                .AddSingleton<ChatSchema>()
                .AddGraphQL((options, provider) =>
                {
                    options.EnableMetrics = Environment.IsDevelopment();
                    options.ExposeExceptions = Environment.IsDevelopment();
                    var logger = provider.GetRequiredService<ILogger<Startup>>();
                    options.UnhandledExceptionDelegate = ctx => logger.LogError("{Error} occured", ctx.OriginalException.Message);
                })
#if NETCOREAPP2_2
                .AddNewtonsoftJson(deserializerSettings => { }, serializerSettings => { })
#else
                .AddSystemTextJson(deserializerSettings => { }, serializerSettings => { })
#endif
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
                BetaUpdates = true,
                RequestCredentials = RequestCredentials.Omit,
                HideTracingResponse = false,

                EditorCursorShape = EditorCursorShape.Line,
                EditorTheme = EditorTheme.Light,
                EditorFontSize = 14,
                EditorReuseHeaders = true,
                EditorFontFamily = "Consolas",

                PrettierPrintWidth = 80,
                PrettierTabWidth = 2,
                PrettierUseTabs = true,
              
                SchemaDisableComments = false,
                SchemaPollingEnabled = true,
                SchemaPollingEndpointFilter = "*localhost*",
                SchemaPollingInterval = 5000,

                Headers = new Dictionary<string, object>
                {
                    ["MyHeader1"] = "MyValue",
                    ["MyHeader2"] = 42,
                },
            });

            app.UseGraphiQLServer(new GraphiQLOptions
            {
                Path = "/ui/graphiql",
                GraphQLEndPoint = "/graphql",
            });

            app.UseGraphQLAltair(new GraphQLAltairOptions
            {
                Path = "/ui/altair",
                GraphQLEndPoint = "/graphql",
                Headers = new Dictionary<string, string>
                {
                    ["X-api-token"] = "130fh9823bd023hd892d0j238dh",
                }
            });

            app.UseGraphQLVoyager(new GraphQLVoyagerOptions
            {
                Path = "/ui/voyager",
                GraphQLEndPoint = "/graphql",
                Headers = new Dictionary<string, object>
                {
                    ["MyHeader1"] = "MyValue",
                    ["MyHeader2"] = 42,
                },
            });
        }
    }
}
