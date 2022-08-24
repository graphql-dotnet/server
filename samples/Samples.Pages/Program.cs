using GraphQL;
using Chat = GraphQL.Samples.Schemas.Chat;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
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
app.UseRouting();
app.MapRazorPages();

await app.RunAsync();
