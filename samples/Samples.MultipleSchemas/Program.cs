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
// configure GraphiQL at "/cats/ui/graphiql" with relative link to api
app.UseGraphQLGraphiQL(
    "/cats/ui/graphiql",
    new GraphQL.Server.Ui.GraphiQL.GraphiQLOptions
    {
        GraphQLEndPoint = "../graphql",
        SubscriptionsEndPoint = "../graphql",
    });
// configure GraphiQL at "/dogs/ui/graphiql" with relative link to api
app.UseGraphQLGraphiQL(
    "/dogs/ui/graphiql",
    new GraphQL.Server.Ui.GraphiQL.GraphiQLOptions
    {
        GraphQLEndPoint = "../graphql",
        SubscriptionsEndPoint = "../graphql",
    });
app.MapRazorPages();
await app.RunAsync();
