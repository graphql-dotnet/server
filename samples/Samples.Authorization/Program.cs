using AuthorizationSample.Data;
using AuthorizationSample.Schema;
using GraphQL;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(
    options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 4;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddAuthorization(c => c.AddPolicy("MyPolicy", cp => cp.RequireRole("User")));
builder.Services.AddRazorPages();

// ---- added code for GraphQL --------
builder.Services.AddGraphQL(b => b
    .AddAutoSchema<Query>()
    .AddSystemTextJson()
    .AddAuthorization());
// ------------------------------------

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// ------ added code for GraphQL -------
app.UseWebSockets();
// configure the graphql endpoint at "/graphql"
app.UseGraphQL("/graphql");
// configure Playground at "/ui/graphql"
app.UseGraphQLPlayground(
    new GraphQL.Server.Ui.Playground.PlaygroundOptions
    {
        GraphQLEndPoint = "/graphql",
        SubscriptionsEndPoint = "/graphql",
        RequestCredentials = GraphQL.Server.Ui.Playground.RequestCredentials.Include,
    },
    "/ui/graphql");
// -------------------------------------

app.MapRazorPages();

app.Run();
