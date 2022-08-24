using GraphQL;
using Chat = GraphQL.Samples.Schemas.Chat;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<Chat.IChat, Chat.Chat>();
builder.Services.AddGraphQL(b => b
    .AddAutoSchema<Chat.Query>(s => s
        .WithMutation<Chat.Mutation>()
        .WithSubscription<Chat.Subscription>())
    .AddSystemTextJson());

var app = builder.Build();
app.UseDeveloperExceptionPage();
app.UseWebSockets();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await app.RunAsync();
