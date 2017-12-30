GraphQL for .NET - Subscription Transport WebSockets
====================================================

>WARNING: not tested in heavy production use! That said if you are using this in production
>drop me a line to tell me how's it working for you. Maybe I can take this disclaimer off.

[![Build status](https://ci.appveyor.com/api/projects/status/x0nf67vfao60wf7e/branch/master?svg=true)](https://ci.appveyor.com/project/graphql-dotnet-ci/server/branch/master)

Transport compatible with [Apollo](https://github.com/apollographql/subscriptions-transport-ws) subscription protocol.

## Getting started

Add graphql-dotnet MyGet feed to your nuget.config
>https://myget.org/F/graphql-dotnet/api/v3/index.json

For just the ASP.NET Core middleware:
>`dotnet add GraphQL.Server.Transports.AspNetCore`

For WebSocket subscription protocol (depends on above)
>`dotnet add GraphQL.Server.Transports.WebSockets`

### Configure

``` csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton<ChatSchema>();

    // add http transport    
    services.AddGraphQLHttp();

    // add websocket transport for ChatSchema
    services.AddGraphQLWebSocket<ChatSchema>();
}

public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    // this is required for websockets support
    app.UseWebSockets();

    // add websocket for ChatSchema at default url /graphql
    app.UseGraphQLWebSocket<ChatSchema>(new GraphQLWebSocketsOptions());

    // add http for ChatSchema at default url /graphql
    app.UseGraphQLHttp<ChatSchema>(new GraphQLHttpOptions());

    // use graphql-playground at default url /ui/playground
    app.UseGraphQLPlayground(new GraphQLPlaygroundOptions());
}

```

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
