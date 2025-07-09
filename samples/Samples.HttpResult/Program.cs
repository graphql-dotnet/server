using GraphQL;
using GraphQL.Server.Transports.AspNetCore;
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

// configure the graphql endpoint at "/graphql", using GraphQLExecutionHttpResult
// map GET in order to support both GET and WebSocket requests
app.MapGet("/graphql", () => new GraphQLExecutionHttpResult());
// map POST to handle standard GraphQL POST requests
app.MapPost("/graphql", () => new GraphQLExecutionHttpResult());

// configure GraphiQL at "/"
app.UseGraphQLGraphiQL(
    "/",
    new GraphQL.Server.Ui.GraphiQL.GraphiQLOptions
    {
        GraphQLEndPoint = "/graphql",
        SubscriptionsEndPoint = "/graphql",
    });

await app.RunAsync();
