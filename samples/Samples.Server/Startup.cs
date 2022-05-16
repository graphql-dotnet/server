using GraphQL.DataLoader;
using GraphQL.Execution;
using GraphQL.MicrosoftDI;
using GraphQL.Samples.Schemas.Chat;
using GraphQL.Server;
using GraphQL.Server.Authorization.AspNetCore;
using GraphQL.Server.Ui.Altair;
using GraphQL.Server.Ui.GraphiQL;
using GraphQL.Server.Ui.Playground;
using GraphQL.Server.Ui.Voyager;
using GraphQL.SystemTextJson;

namespace GraphQL.Samples.Server;

public class Startup
{
    public Startup(IConfiguration configuration, IWebHostEnvironment environment)
    {
        Configuration = configuration;
        Environment = environment;
    }

    public IConfiguration Configuration { get; }

    public IWebHostEnvironment Environment { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSingleton<IChat, Chat>()
            .Configure<ErrorInfoProviderOptions>(opt => opt.ExposeExceptionStackTrace = Environment.IsDevelopment())
            .AddTransient<IAuthorizationErrorMessageBuilder, DefaultAuthorizationErrorMessageBuilder>(); // required by CustomErrorInfoProvider

        services.AddGraphQL(builder => builder
            .AddApolloTracing()
            .AddHttpMiddleware<ChatSchema, GraphQLHttpMiddlewareWithLogs<ChatSchema>>()
            .AddWebSocketsHttpMiddleware<ChatSchema>()
            .AddSchema<ChatSchema>()
            .ConfigureExecutionOptions(options =>
            {
                options.EnableMetrics = Environment.IsDevelopment();
                var logger = options.RequestServices.GetRequiredService<ILogger<Startup>>();
                options.UnhandledExceptionDelegate = ctx =>
                {
                    logger.LogError("{Error} occurred", ctx.OriginalException.Message);
                    return Task.CompletedTask;
                };
            })
            .AddSystemTextJson()
            .AddErrorInfoProvider<CustomErrorInfoProvider>()
            .AddWebSockets()
            .AddDataLoader()
            .AddGraphTypes(typeof(ChatSchema).Assembly));
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app)
    {
        if (Environment.IsDevelopment())
            app.UseDeveloperExceptionPage();

        app.UseWebSockets();

        app.UseGraphQLWebSockets<ChatSchema>();
        app.UseGraphQL<ChatSchema, GraphQLHttpMiddlewareWithLogs<ChatSchema>>();

        app.UseGraphQLPlayground(new PlaygroundOptions
        {
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

        app.UseGraphQLGraphiQL(new GraphiQLOptions
        {
            Headers = new Dictionary<string, string>
            {
                ["X-api-token"] = "130fh9823bd023hd892d0j238dh",
            }
        });

        app.UseGraphQLAltair(new AltairOptions
        {
            Headers = new Dictionary<string, string>
            {
                ["X-api-token"] = "130fh9823bd023hd892d0j238dh",
            }
        });

        app.UseGraphQLVoyager(new VoyagerOptions
        {
            Headers = new Dictionary<string, object>
            {
                ["MyHeader1"] = "MyValue",
                ["MyHeader2"] = 42,
            },
        });
    }
}
