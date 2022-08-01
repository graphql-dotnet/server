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
    .AddSystemTextJson()
    // support authorization policies within the schema (although none are set in this sample)
    .AddAuthorizationRule()
    // support WebSocket authentication via the payload of the initialization packet
    .AddWebSocketAuthentication<JwtWebSocketAuthenticationService>());

// provide authentication for GET/POST requests via the 'Authorization' HTTP header;
// will authenticate WebSocket requests as well, but browsers cannot set the
// 'Authorization' HTTP header for WebSocket requests
builder.Services.AddAuthentication(
    opts =>
    {
        opts.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        // configure custom authorization policies here
    })
    .AddJwtBearer(opts => opts.TokenValidationParameters = JwtHelper.TokenValidationParameters);

var app = builder.Build();
app.UseDeveloperExceptionPage();
app.UseWebSockets();
// use ASP.Net Core authentication (again, for GET/POST requests mainly)
app.UseAuthentication();
// configure the graphql endpoint at "/graphql" for administrators only
app.UseGraphQL("/graphql", opts =>
{
    // set a transport-level authorization policy; connections are refused if this policy is not met
    // this means that anonymous access to the introspection query is not available
    opts.AuthorizedRoles.Add("Administrator");
});
app.UseRouting();
// enable Pages for the GraphiQL demonstration endpoint at /
app.MapRazorPages();
// the OAuth2 authorization endpoint will be at /token
app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action}");

await app.RunAsync();
