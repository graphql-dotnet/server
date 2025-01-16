using GraphQL;
using GraphQL.Server.Samples.NativeAot;
using GraphQL.Server.Samples.NativeAot.GraphTypes;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddGraphQL(b => b
    .AddSchema<MySchema>()
    .AddSystemTextJson());

builder.Services.AddTransient<QueryType>();

var app = builder.Build();

app.UseGraphQLGraphiQL("/");
app.UseGraphQL();

app.Run();
