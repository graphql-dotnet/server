using GraphQL.MicrosoftDI;
using GraphQL.SystemTextJson;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddSingleton<MultipleSchema.Cats.CatsData>();
builder.Services.AddSingleton<MultipleSchema.Dogs.DogsData>();
builder.Services.AddGraphQL(b => b
    .AddSchema<MultipleSchema.Cats.CatsSchema>()
    .AddSchema<MultipleSchema.Dogs.DogsSchema>()
    .AddAutoClrMappings()
    .AddSystemTextJson());

var app = builder.Build();
app.UseDeveloperExceptionPage();
app.UseWebSockets();
// configure the graphql endpoint at "/cats/graphql"
app.UseGraphQL<MultipleSchema.Cats.CatsSchema>("/cats/graphql");
// configure the graphql endpoint at "/dogs/graphql"
app.UseGraphQL<MultipleSchema.Dogs.DogsSchema>("/dogs/graphql");
// configure Playground at "/cats"
app.UseGraphQLPlayground(
    new GraphQL.Server.Ui.Playground.PlaygroundOptions
    {
        GraphQLEndPoint = new PathString("/cats/graphql"),
        SubscriptionsEndPoint = new PathString("/cats/graphql"),
    },
    "/cats");
// configure Playground at "/dogs"
app.UseGraphQLPlayground(
    new GraphQL.Server.Ui.Playground.PlaygroundOptions
    {
        GraphQLEndPoint = new PathString("/dogs/graphql"),
        SubscriptionsEndPoint = new PathString("/dogs/graphql"),
    },
    "/dogs");
app.MapRazorPages();
await app.RunAsync();
