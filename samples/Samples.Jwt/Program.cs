using GraphQL;
using GraphQL.Server.Samples.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Chat = GraphQL.Samples.Schemas.Chat;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddSingleton<Chat.IChat, Chat.Chat>();
builder.Services.AddGraphQL(b => b
    .AddAutoSchema<Chat.Query>(s => s
        .WithMutation<Chat.Mutation>()
        .WithSubscription<Chat.Subscription>())
    .AddAuthorizationRule()
    .AddWebSocketAuthentication<JwtWebSocketAuthenticationService>()
    .AddSystemTextJson());

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts => opts.TokenValidationParameters = JwtHelper.TokenValidationParameters);

var app = builder.Build();
app.UseDeveloperExceptionPage();
app.UseWebSockets();
app.UseAuthentication();
// configure the graphql endpoint at "/graphql" for administrators only
app.UseGraphQL("/graphql", opts => opts.AuthorizedRoles.Add("Administrator"));
app.UseRouting();
app.MapRazorPages();
// the authorization endpoint will be at /token
app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action}");

await app.RunAsync();
