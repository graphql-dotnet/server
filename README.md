GraphQL for .NET - Subscription Transport WebSockets
====================================================

[![Build status](https://ci.appveyor.com/api/projects/status/x0nf67vfao60wf7e/branch/master?svg=true)](https://ci.appveyor.com/project/graphql-dotnet-ci/server/branch/master)

![Activity](https://img.shields.io/github/commit-activity/w/graphql-dotnet/server)
![Activity](https://img.shields.io/github/commit-activity/m/graphql-dotnet/server)
![Activity](https://img.shields.io/github/commit-activity/y/graphql-dotnet/server)

![Size](https://img.shields.io/github/repo-size/graphql-dotnet/server)

Provides the following packages:

| Package | Downloads | Nuget Latest |
|---------|-----------|--------------|
| GraphQL.Server.Core | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Core)](https://www.nuget.org/packages/GraphQL.Server.Core/) | [![Nuget](https://img.shields.io/nuget/v/GraphQL.Server.Core)](https://www.nuget.org/packages/GraphQL.Server.Core) |
| GraphQL.Server.Transports.AspNetCore | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Transports.AspNetCore)](https://www.nuget.org/packages/GraphQL.Server.Transports.AspNetCore) | [![Nuget](https://img.shields.io/nuget/v/GraphQL.Server.Transports.AspNetCore)](https://www.nuget.org/packages/GraphQL.Server.Transports.AspNetCore) |
| GraphQL.Server.Transports.AspNetCore.NewtonsoftJson | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Transports.AspNetCore.NewtonsoftJson)](https://www.nuget.org/packages/GraphQL.Server.Transports.AspNetCore.NewtonsoftJson) | [![Nuget](https://img.shields.io/nuget/v/GraphQL.Server.Transports.AspNetCore.NewtonsoftJson)](https://www.nuget.org/packages/GraphQL.Server.Transports.AspNetCore.NewtonsoftJson) |
| GraphQL.Server.Transports.AspNetCore.SystemTextJson | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Transports.AspNetCore.SystemTextJson)](https://www.nuget.org/packages/GraphQL.Server.Transports.AspNetCore.SystemTextJson) | [![Nuget](https://img.shields.io/nuget/v/GraphQL.Server.Transports.AspNetCore.SystemTextJson)](https://www.nuget.org/packages/GraphQL.Server.Transports.AspNetCore.SystemTextJson) |
| GraphQL.Server.Transports.Subscriptions.Abstractions | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Transports.Subscriptions.Abstractions)](https://www.nuget.org/packages/GraphQL.Server.Transports.Subscriptions.Abstractions) | [![Nuget](https://img.shields.io/nuget/v/GraphQL.Server.Transports.Subscriptions.Abstractions)](https://www.nuget.org/packages/GraphQL.Server.Transports.Subscriptions.Abstractions) |
| GraphQL.Server.Transports.WebSockets | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Transports.WebSockets)](https://www.nuget.org/packages/GraphQL.Server.Transports.WebSockets) | [![Nuget](https://img.shields.io/nuget/v/GraphQL.Server.Transports.WebSockets)](https://www.nuget.org/packages/GraphQL.Server.Transports.WebSockets) |
| GraphQL.Server.Ui.Altair | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Ui.Altair)](https://www.nuget.org/packages/GraphQL.Server.Ui.Altair) | [![Nuget](https://img.shields.io/nuget/v/GraphQL.Server.Ui.Altair)](https://www.nuget.org/packages/GraphQL.Server.Ui.Altair) |
| GraphQL.Server.Ui.Playground | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Ui.Playground)](https://www.nuget.org/packages/GraphQL.Server.Ui.Playground) | [![Nuget](https://img.shields.io/nuget/v/GraphQL.Server.Ui.Playground)](https://www.nuget.org/packages/GraphQL.Server.Ui.Playground) |
| GraphQL.Server.Ui.GraphiQL | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Ui.GraphiQL)](https://www.nuget.org/packages/GraphQL.Server.Ui.GraphiQL) | [![Nuget](https://img.shields.io/nuget/v/GraphQL.Server.Ui.GraphiQL)](https://www.nuget.org/packages/GraphQL.Server.Ui.GraphiQL) |
| GraphQL.Server.Ui.Voyager | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Ui.Voyager)](https://www.nuget.org/packages/GraphQL.Server.Ui.Voyager) | [![Nuget](https://img.shields.io/nuget/v/GraphQL.Server.Ui.Voyager)](https://www.nuget.org/packages/GraphQL.Server.Ui.Voyager) |
| GraphQL.Server.Authorization.AspNetCore | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Authorization.AspNetCore)](https://www.nuget.org/packages/GraphQL.Server.Authorization.AspNetCore) | [![Nuget](https://img.shields.io/nuget/v/GraphQL.Server.Authorization.AspNetCore)](https://www.nuget.org/packages/GraphQL.Server.Authorization.AspNetCore) |

Transport compatible with [Apollo](https://github.com/apollographql/subscriptions-transport-ws) subscription protocol.

## Getting started

> NOTE: You can see the changes in public APIs using [fuget.org](https://www.fuget.org/packages/GraphQL.Server.Transports.AspNetCore/4.0.1/lib/netstandard2.0/diff/3.4.0/).

You can install the latest stable version via [NuGet](https://www.nuget.org/packages/GraphQL.Server.Transports.AspNetCore/).
```
> dotnet add package GraphQL.Server.Transports.AspNetCore
```

You can get the latest pre-release packages from the [MyGet feed](https://www.myget.org/F/graphql-dotnet/api/v3/index.json),
where you may want to explicitly pull a certain version using `-v`.
```
> dotnet add package GraphQL.Server.Transports.AspNetCore -v 3.5.0-alpha0071
```

For just the HTTP middleware:
>`dotnet add package GraphQL.Server.Transports.AspNetCore`

The HTTP middleware needs an `IGraphQLRequestDeserializer` implementation:
> .NET Core 3+:  
> `dotnet add package GraphQL.Server.Transports.AspNetCore.SystemTextJson`  
> Legacy (prior to .NET Core 3):  
> `dotnet add package GraphQL.Server.Transports.AspNetCore.NewtonsoftJson`  
> (or your own)

For more information on how to migrate from Newtonsoft.Json to System.Text.Json see
[this article](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-migrate-from-newtonsoft-how-to).

For the WebSocket subscription protocol (depends on above) middleware:
>`dotnet add package GraphQL.Server.Transports.WebSockets`

For the UI middleware/s:
>`dotnet add package GraphQL.Server.Ui.Altair`  
>`dotnet add package GraphQL.Server.Ui.GraphiQL`  
>`dotnet add package GraphQL.Server.Ui.Playground`  
>`dotnet add package GraphQL.Server.Ui.Voyager`  

### Configure

See the sample project's Startup.cs for full details.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Add GraphQL services and configure options
    services
        .AddSingleton<IChat, Chat>()
        .AddSingleton<ChatSchema>()
        .AddGraphQL((options, provider) =>
        {
            options.EnableMetrics = Environment.IsDevelopment();
            var logger = provider.GetRequiredService<ILogger<Startup>>();
            options.UnhandledExceptionDelegate = ctx => logger.LogError("{Error} occured", ctx.OriginalException.Message);
        })
        // Add required services for de/serialization
        .AddSystemTextJson(deserializerSettings => { }, serializerSettings => { }) // For .NET Core 3+
        .AddNewtonsoftJson(deserializerSettings => { }, serializerSettings => { }) // For everything else
        .AddErrorInfoProvider(opt => opt.ExposeExceptionStackTrace = Environment.IsDevelopment())
        .AddWebSockets() // Add required services for web socket support
        .AddDataLoader() // Add required services for DataLoader support
        .AddGraphTypes(typeof(ChatSchema)) // Add all IGraphType implementors in assembly which ChatSchema exists 
}

public void Configure(IApplicationBuilder app)
{
    // this is required for websockets support
    app.UseWebSockets();

    // use websocket middleware for ChatSchema at path /graphql
    app.UseGraphQLWebSockets<ChatSchema>("/graphql");

    // use HTTP middleware for ChatSchema at path /graphql
    app.UseGraphQL<ChatSchema>("/graphql");

    // use graphiQL middleware at default url /ui/graphiql
    app.UseGraphiQLServer();

    // use graphql-playground middleware at default url /ui/playground
    app.UseGraphQLPlayground();

    // use altair middleware at default url /ui/altair
    app.UseGraphQLAltair();
    
    // use voyager middleware at default url /ui/voyager
    app.UseGraphQLVoyager();
}
```

### UserContext and resolvers

`UserContext` of your resolver will be type of `MessageHandlingContext`. You can
access the properties including your actual `UserContext` by using the
`Get<YourContextType>("UserContext")` method. This will read the context from the properties of
`MessageHandlingContext`. You can add any other properties as to the context in
`IOperationMessageListeners`. See the sample for example of injecting `ClaimsPrincipal`.

## Sample

Samples.Server shows a simple Chat example demonstrating the subscription transport.
It can be run as `netcoreapp2.2`, `netcoreapp3.0` or `netcoreapp3.1`, and supports
various GraphQL client IDEs (by default opening GraphQL Playground).

Here are some example queries to get started. Use three browser tabs or better yet windows 
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
