using GraphQL;
using Samples.Upload;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddGraphQL(b => b
    .AddAutoSchema<Query>(c => c.WithMutation<Mutation>())
    .AddFormFileGraphType()
    .AddSystemTextJson());

var app = builder.Build();
app.UseDeveloperExceptionPage();
app.UseGraphQL();
app.UseRouting();
app.MapRazorPages();

await app.RunAsync();
