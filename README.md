GraphQL for .NET - Subscription Transport WebSockets
====================================================

>WARNING: not ready for production use!

[![Build status](https://ci.appveyor.com/api/projects/status/11e1i3yp07jy18jj/branch/master?svg=true)](https://ci.appveyor.com/project/graphql-dotnet-ci/subscriptions-transport-ws/branch/master)

Transport compatible with [Apollo](https://github.com/apollographql/subscriptions-transport-ws) subscription protocol.

## Getting started

>`install package`

### Configure

``` csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton<ChatSchema>();

    // Http transport using json
    services.AddGraphQLHttpTransport<ChatSchema>();

    // WebSockets transport using subscription-transport-ws
    services.AddGraphQLWebSocketsTransport<ChatSchema>();

    // common GraphQL services
    services.AddGraphQL();
}

public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    // this is required for websockets support
    app.UseWebSockets();

    // registers schema as endpoint handling both
    // json requests and websocket requests
    app.UseGraphQLEndPoint<ChatSchema>("/graphql");
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
