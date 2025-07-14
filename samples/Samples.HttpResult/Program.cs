using GraphQL;
using GraphQL.Server.Transports.AspNetCore;
using GraphQL.Transport;
using GraphQL.Types;
using Chat = GraphQL.Samples.Schemas.Chat;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<Chat.IChat, Chat.Chat>();
builder.Services.AddGraphQL(b => b
    .AddAutoSchema<Chat.Query>(s => s
        .WithMutation<Chat.Mutation>()
        .WithSubscription<Chat.Subscription>())
    .AddSystemTextJson());

var app = builder.Build();
app.UseDeveloperExceptionPage();
app.UseWebSockets();

// configure the graphql endpoint at "/graphql", using GraphQLExecutionHttpResult
// map GET in order to support both GET and WebSocket requests
app.MapGet("/graphql", () => new GraphQLExecutionHttpResult());
// map POST to handle standard GraphQL POST requests
app.MapPost("/graphql", () => new GraphQLExecutionHttpResult());

// Example endpoint demonstrating ExecutionResultHttpResult with custom logic
app.MapPost("/graphql-result", async (HttpContext context, IDocumentExecuter<ISchema> documentExecuter, IGraphQLTextSerializer serializer) =>
{
    GraphQLRequest? request;

    if (context.Request.HasFormContentType)
    {
        var form = await context.Request.ReadFormAsync(context.RequestAborted);
        request = new GraphQLRequest
        {
            Query = form["query"].ToString() == "" ? null : form["query"].ToString(),
            DocumentId = form["documentId"].ToString() == "" ? null : form["documentId"].ToString(),
            OperationName = form["operationName"].ToString() == "" ? null : form["operationName"].ToString(),
            Variables = serializer.Deserialize<Inputs>(form["variables"].ToString() == "" ? null : form["variables"].ToString()),
            Extensions = serializer.Deserialize<Inputs>(form["extensions"].ToString() == "" ? null : form["extensions"].ToString()),
        };
    }
    else if (context.Request.HasJsonContentType())
    {
        request = await serializer.ReadAsync<GraphQLRequest>(context.Request.Body, context.RequestAborted);
    }
    else
    {
        return Results.BadRequest();
    }

    var opts = new ExecutionOptions
    {
        Query = request?.Query,
        DocumentId = request?.DocumentId,
        OperationName = request?.OperationName,
        Variables = request?.Variables,
        Extensions = request?.Extensions,
        CancellationToken = context.RequestAborted,
        RequestServices = context.RequestServices,
        User = context.User,
    };

    var result = await documentExecuter.ExecuteAsync(opts);
    return new ExecutionResultHttpResult(result);
});

// configure GraphiQL at "/"
app.UseGraphQLGraphiQL(
    "/",
    new GraphQL.Server.Ui.GraphiQL.GraphiQLOptions
    {
        GraphQLEndPoint = "/graphql",
        SubscriptionsEndPoint = "/graphql",
    });

await app.RunAsync();
