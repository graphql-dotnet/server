using GraphQL;
using Chat = GraphQL.Samples.Schemas.Chat;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<Chat.IChat, Chat.Chat>();
builder.Services.AddGraphQL(b => b
    .AddAutoSchema<Chat.Query>(s => s
        .WithMutation<Chat.Mutation>()
        .WithSubscription<Chat.Subscription>())
    .AddSystemTextJson());

var app = builder.Build();
app.UseDeveloperExceptionPage();
app.UseWebSockets();
// configure the graphql endpoint at "/graphql"
app.UseGraphQL("/graphql");
// configure Playground at "/"
app.UseGraphQLPlayground(
    new GraphQL.Server.Ui.Playground.PlaygroundOptions
    {
        GraphQLEndPoint = "/graphql",
        SubscriptionsEndPoint = "/graphql",
    },
    "/");

await app.RunAsync();
