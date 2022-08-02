using GraphQL;
using GraphQL.Server.Samples.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Chat = GraphQL.Samples.Schemas.Chat;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddSingleton<Chat.IChat, Chat.Chat>();
builder.Services.AddGraphQL(b => b
    .AddAutoSchema<Chat.Query>(s => s
        .WithMutation<Chat.Mutation>()
        .WithSubscription<Chat.Subscription>())
    .AddSystemTextJson()
    // support authorization policies within the schema (although none are set in this sample)
    .AddAuthorizationRule()
    // support WebSocket authentication via the payload of the initialization message
    .AddWebSocketAuthentication<JwtWebSocketAuthenticationService>());

// configure authentication for GET/POST requests via the 'Authorization' HTTP header;
// will authenticate WebSocket requests as well, but browsers cannot set the
// 'Authorization' HTTP header for WebSocket requests
builder.Services.AddAuthentication(
    opts =>
    {
        opts.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        // configure custom authorization policies here, if any
    })
    .AddJwtBearer(opts => opts.TokenValidationParameters = JwtHelper.TokenValidationParameters);

var app = builder.Build();
app.UseDeveloperExceptionPage();
// enable WebSocket support within the ASP.NET Core framework
app.UseWebSockets();
// use ASP.Net Core authentication (again, for GET/POST requests mainly)
app.UseAuthentication();
// configure the graphql endpoint at "/graphql" for administrators only
app.UseGraphQL("/graphql", opts =>
{
    // here we are setting a transport-level authorization policy allowing only authenticated users
    // that carry the "Administrator" role to connect; connections are refused if this policy is not met

    // because of this rule, anonymous access to perform introspection queries is not available

    // remove this authorization policy if you wish to allow anonymous access to your schema and set
    // authorization policies on individual graphs and fields

    opts.AuthorizedRoles.Add("Administrator");
});
// enable ASP.Net Core routing for razor pages and MVC controllers
app.UseRouting();
// enable Pages for the GraphiQL demonstration endpoint at /
app.MapRazorPages();
// enable MVC controllers for the OAuth2 authorization endpoint at /token
app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action}");

await app.RunAsync();
