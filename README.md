GraphQL for .NET - Subscription Transport WebSockets
====================================================

>WARNING: not tested in heavy production use! That said if you are using this in production
>drop me a line to tell me how's it working for you. Maybe I can take this disclaimer off.

[![Build status](https://ci.appveyor.com/api/projects/status/x0nf67vfao60wf7e/branch/master?svg=true)](https://ci.appveyor.com/project/graphql-dotnet-ci/server/branch/master)

Transport compatible with [Apollo](https://github.com/apollographql/subscriptions-transport-ws) subscription protocol.

## Getting started

For just the HTTP middleware:
>`dotnet add package GraphQL.Server.Transports.AspNetCore`

For the WebSocket subscription protocol (depends on above) middleware:
>`dotnet add package GraphQL.Server.Transports.WebSockets`

For the UI middleware/s:
>`dotnet add package GraphQL.Server.Ui.GraphiQL`
>`dotnet add package GraphQL.Server.Ui.Playground`
>`dotnet add package GraphQL.Server.Ui.Voyager`


### Configure

``` csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton<ChatSchema>();

    // add http transport    
    services.AddGraphQLHttp();
    
    // setup execution options for ChatSchema
    services.Configure<ExecutionOptions<ChatSchema>>(options =>
            {
                options.EnableMetrics = true;
                options.ExposeExceptions = true;
                options.UserContext = "something";
            });

    // add websocket transport for ChatSchema
    services.AddGraphQLWebSocket<ChatSchema>();
}

public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    // this is required for websockets support
    app.UseWebSockets();

    // use websocket middleware for ChatSchema at default url /graphql
    app.UseGraphQLWebSocket<ChatSchema>(new GraphQLWebSocketsOptions());

    // use http middleware for ChatSchema at default url /graphql
    app.UseGraphQLHttp<ChatSchema>(new GraphQLHttpOptions());

    // use graphiQL middleware at default url /graphiql
    app.UseGraphiQLServer(new GraphiQLOptions());

    // use graphql-playground middleware at default url /ui/playground
    app.UseGraphQLPlayground(new GraphQLPlaygroundOptions());
    
    // use voyager middleware at default url /ui/voyager
    app.UseGraphQLVoyager(new GraphQLVoyagerOptions());
}

```

### UserContext and resolvers

`UserContext` of your resolver will be type of `MessageHandlingContext`. You can
access the properties including your actual `UserContext` by using the
`Get<YourContextType>("UserContext")` method. This will read the context from the properties of
`MessageHandlingContext`. You can add any other properties as to the context in
`IOperationMessageListeners`. See the sample for example of injecting `ClaimsPrincipal`.


## Sample

Samples.Server shows a simple Chat style example of how subscription transport is used
with GraphiQL integration.

Here are example queries to get started. Use three browser tabs or better yet windows 
to view the changes.

### Subscription 1

Query:

```
subscription MessageAddedByUser($id:String!) {
  messageAddedByUser(id: $id) {
    from { id displayName }
    content
  }
}
```

Variables:

```
{
  "id": "1"
}
```

### Subscription 2

```
subscription MessageAdded {
  messageAdded {
    from { id displayName }
    content
  }
}
```

### Mutation

Query:

```
mutation AddMessage($message: MessageInputType!) {
  addMessage(message: $message) {
    from {
      id
      displayName
    }
    content
  }
}
```

Variables: 

```
{
  "message": {
    "content": "Message",
    "fromId": "1"
  }
}
```
