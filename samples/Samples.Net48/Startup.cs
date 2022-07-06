using Chat = GraphQL.Samples.Schemas.Chat;

namespace GraphQL.Server.Samples.Net48;

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
        services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

        services.AddSingleton<Chat.IChat, Chat.Chat>();
        services.AddGraphQL(b => b
            .AddAutoSchema<Chat.Query>(s => s
                .WithMutation<Chat.Mutation>()
                .WithSubscription<Chat.Subscription>())
            .AddNewtonsoftJson());
        services.AddHostApplicationLifetime();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
        app.UseDeveloperExceptionPage();
        app.UseWebSockets();
        app.UseMvc();

        // configure the graphql endpoint at "/graphql"
        app.UseGraphQL("/graphql");
        // configure the GraphiQL endpoint at "/ui/graphql"
        app.UseGraphQLGraphiQL(
            new Ui.GraphiQL.GraphiQLOptions
            {
                GraphQLEndPoint = "/graphql",
                SubscriptionsEndPoint = "/graphql",
            },
            "/ui/graphql");
    }
}
