GraphQL for .NET - Subscription Transport WebSockets
====================================================

>WARNING: not tested in heavy production use! That said if you are using this in production
>drop me a line to tell me how's it working for you. Maybe I can take this disclaimer off.

[![Build status](https://ci.appveyor.com/api/projects/status/x0nf67vfao60wf7e/branch/master?svg=true)](https://ci.appveyor.com/project/graphql-dotnet-ci/server/branch/master)

Provides the following packages:

| Package | Downloads |
|---------|-----------|
| GraphQL.Server.Core | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Core)](https://www.nuget.org/packages/GraphQL.Server.Core/) |
| GraphQL.Server.Transports.AspNetCore | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Transports.AspNetCore)](https://www.nuget.org/packages/GraphQL.Server.Transports.AspNetCore/) |
| GraphQL.Server.Transports.Subscriptions.Abstractions | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Transports.Subscriptions.Abstractions)](https://www.nuget.org/packages/GraphQL.Server.Transports.Subscriptions.Abstractions/) |
| GraphQL.Server.Transports.WebSockets | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Transports.WebSockets)](https://www.nuget.org/packages/GraphQL.Server.Transports.WebSockets/) |
| GraphQL.Server.Ui.Playground | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Ui.Playground)](https://www.nuget.org/packages/GraphQL.Server.Ui.Playground/) |
| GraphQL.Server.Ui.GraphiQL | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Ui.GraphiQL)](https://www.nuget.org/packages/GraphQL.Server.Ui.GraphiQL/) |
| GraphQL.Server.Ui.Altair | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Ui.Altair)](https://www.nuget.org/packages/GraphQL.Server.Ui.Altair/) |
| GraphQL.Server.Ui.Voyager | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Ui.Voyager)](https://www.nuget.org/packages/GraphQL.Server.Ui.Voyager/) |
| GraphQL.Server.Authorization.AspNetCore | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Authorization.AspNetCore)](https://www.nuget.org/packages/GraphQL.Server.Authorization.AspNetCore/) |

Transport compatible with [Apollo](https://github.com/apollographql/subscriptions-transport-ws) subscription protocol.

## Getting started

For just the HTTP middleware:
>`dotnet add package GraphQL.Server.Transports.AspNetCore`

For the WebSocket subscription protocol (depends on above) middleware:
>`dotnet add package GraphQL.Server.Transports.WebSockets`

For the UI middleware/s:
>`dotnet add package GraphQL.Server.Ui.GraphiQL`  
>`dotnet add package GraphQL.Server.Ui.Playground`  
>`dotnet add package GraphQL.Server.Ui.Altair`  
>`dotnet add package GraphQL.Server.Ui.Voyager`  


### Configure

``` csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton<ChatSchema>();

    // Add GraphQL services and configure options
    services.AddGraphQL(options =>
    {
        options.EnableMetrics = true;
        options.ExposeExceptions = this.Environment.IsDevelopment();
    })
    .AddWebSockets() // Add required services for web socket support
    .AddDataLoader(); // Add required services for DataLoader support
}

public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    // this is required for websockets support
    app.UseWebSockets();

    // use websocket middleware for ChatSchema at path /graphql
    app.UseGraphQLWebSockets<ChatSchema>("/graphql");

    // use HTTP middleware for ChatSchema at path /graphql
    app.UseGraphQL<ChatSchema>("/graphql");

    // use graphiQL middleware at default url /graphiql
    app.UseGraphiQLServer(new GraphiQLOptions());

    // use graphql-playground middleware at default url /ui/playground
    app.UseGraphQLPlayground(new GraphQLPlaygroundOptions());

    // use altair middleware at default url /ui/altair
    app.UseGraphQLAltair(new GraphQLAltairOptions());
    
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
with GraphiQL integration. An example works for `netcoreapp2.2` and `netcoreapp3.0` platforms.

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
