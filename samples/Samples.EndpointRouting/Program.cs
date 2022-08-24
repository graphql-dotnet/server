using GraphQL;
using Chat = GraphQL.Samples.Schemas.Chat;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<Chat.IChat, Chat.Chat>();
builder.Services.AddGraphQL(b => b
    .AddAutoSchema<Chat.Query>(s => s
        .WithMutation<Chat.Mutation>()
        .WithSubscription<Chat.Subscription>())
    .AddSystemTextJson());
builder.Services.AddRouting();

var app = builder.Build();
app.UseDeveloperExceptionPage();
app.UseWebSockets();
app.UseRouting();
app.UseEndpoints(endpoints =>
{
    // configure the graphql endpoint at "/graphql"
    endpoints.MapGraphQL("/graphql");
    // configure Playground at "/"
    endpoints.MapGraphQLPlayground("/");
});
await app.RunAsync();
