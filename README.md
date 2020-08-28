GraphQL for .NET - Subscription Transport WebSockets
====================================================

[![Build status](https://ci.appveyor.com/api/projects/status/x0nf67vfao60wf7e/branch/master?svg=true)](https://ci.appveyor.com/project/graphql-dotnet-ci/server/branch/master)

![Activity](https://img.shields.io/github/commit-activity/w/graphql-dotnet/server)
![Activity](https://img.shields.io/github/commit-activity/m/graphql-dotnet/server)
![Activity](https://img.shields.io/github/commit-activity/y/graphql-dotnet/server)

![Size](https://img.shields.io/github/repo-size/graphql-dotnet/server)

Provides the following packages:

| Package | Downloads | Nuget Latest | MyGet Latest |
|---------|-----------|--------------|--------------|
| GraphQL.Server.Core | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Core)](https://www.nuget.org/packages/GraphQL.Server.Core/) | [![Nuget](https://img.shields.io/nuget/vpre/GraphQL.Server.Core)](https://www.nuget.org/packages/GraphQL.Server.Core) | [![MyGet](https://img.shields.io/myget/graphql-dotnet/vpre/GraphQL.Server.Core?label=myget)](https://www.myget.org/F/graphql-dotnet/api/v3/index.json) |
| GraphQL.Server.Transports.AspNetCore | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Transports.AspNetCore)](https://www.nuget.org/packages/GraphQL.Server.Transports.AspNetCore) | [![Nuget](https://img.shields.io/nuget/vpre/GraphQL.Server.Transports.AspNetCore)](https://www.nuget.org/packages/GraphQL.Server.Transports.AspNetCore) | [![MyGet](https://img.shields.io/myget/graphql-dotnet/vpre/GraphQL.Server.Transports.AspNetCore?label=myget)](https://www.myget.org/F/graphql-dotnet/api/v3/index.json) |
| GraphQL.Server.Transports.AspNetCore.NewtonsoftJson | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Transports.AspNetCore.NewtonsoftJson)](https://www.nuget.org/packages/GraphQL.Server.Transports.AspNetCore.NewtonsoftJson) | [![Nuget](https://img.shields.io/nuget/vpre/GraphQL.Server.Transports.AspNetCore.NewtonsoftJson)](https://www.nuget.org/packages/GraphQL.Server.Transports.AspNetCore.NewtonsoftJson) | [![MyGet](https://img.shields.io/myget/graphql-dotnet/vpre/GraphQL.Server.Transports.AspNetCore.NewtonsoftJson?label=myget)](https://www.myget.org/F/graphql-dotnet/api/v3/index.json) |
| GraphQL.Server.Transports.AspNetCore.SystemTextJson | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Transports.AspNetCore.SystemTextJson)](https://www.nuget.org/packages/GraphQL.Server.Transports.AspNetCore.SystemTextJson) | [![Nuget](https://img.shields.io/nuget/vpre/GraphQL.Server.Transports.AspNetCore.SystemTextJson)](https://www.nuget.org/packages/GraphQL.Server.Transports.AspNetCore.SystemTextJson) | [![MyGet](https://img.shields.io/myget/graphql-dotnet/vpre/GraphQL.Server.Transports.AspNetCore.SystemTextJson?label=myget)](https://www.myget.org/F/graphql-dotnet/api/v3/index.json) |
| GraphQL.Server.Transports.Subscriptions.Abstractions | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Transports.Subscriptions.Abstractions)](https://www.nuget.org/packages/GraphQL.Server.Transports.Subscriptions.Abstractions) | [![Nuget](https://img.shields.io/nuget/vpre/GraphQL.Server.Transports.Subscriptions.Abstractions)](https://www.nuget.org/packages/GraphQL.Server.Transports.Subscriptions.Abstractions) | [![MyGet](https://img.shields.io/myget/graphql-dotnet/vpre/GraphQL.Server.Transports.Subscriptions.Abstractions?label=myget)](https://www.myget.org/F/graphql-dotnet/api/v3/index.json) |
| GraphQL.Server.Transports.WebSockets | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Transports.WebSockets)](https://www.nuget.org/packages/GraphQL.Server.Transports.WebSockets) | [![Nuget](https://img.shields.io/nuget/vpre/GraphQL.Server.Transports.WebSockets)](https://www.nuget.org/packages/GraphQL.Server.Transports.WebSockets) | [![MyGet](https://img.shields.io/myget/graphql-dotnet/vpre/GraphQL.Server.Transports.WebSockets?label=myget)](https://www.myget.org/F/graphql-dotnet/api/v3/index.json) |
| GraphQL.Server.Ui.Altair | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Ui.Altair)](https://www.nuget.org/packages/GraphQL.Server.Ui.Altair) | [![Nuget](https://img.shields.io/nuget/vpre/GraphQL.Server.Ui.Altair)](https://www.nuget.org/packages/GraphQL.Server.Ui.Altair) | [![MyGet](https://img.shields.io/myget/graphql-dotnet/vpre/GraphQL.Server.Ui.Altair?label=myget)](https://www.myget.org/F/graphql-dotnet/api/v3/index.json) |
| GraphQL.Server.Ui.Playground | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Ui.Playground)](https://www.nuget.org/packages/GraphQL.Server.Ui.Playground) | [![Nuget](https://img.shields.io/nuget/vpre/GraphQL.Server.Ui.Playground)](https://www.nuget.org/packages/GraphQL.Server.Ui.Playground) | [![MyGet](https://img.shields.io/myget/graphql-dotnet/vpre/GraphQL.Server.Ui.Playground?label=myget)](https://www.myget.org/F/graphql-dotnet/api/v3/index.json) |
| GraphQL.Server.Ui.GraphiQL | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Ui.GraphiQL)](https://www.nuget.org/packages/GraphQL.Server.Ui.GraphiQL) | [![Nuget](https://img.shields.io/nuget/vpre/GraphQL.Server.Ui.GraphiQL)](https://www.nuget.org/packages/GraphQL.Server.Ui.GraphiQL) | [![MyGet](https://img.shields.io/myget/graphql-dotnet/vpre/GraphQL.Server.Ui.GraphiQL?label=myget)](https://www.myget.org/F/graphql-dotnet/api/v3/index.json) |
| GraphQL.Server.Ui.Voyager | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Ui.Voyager)](https://www.nuget.org/packages/GraphQL.Server.Ui.Voyager) | [![Nuget](https://img.shields.io/nuget/vpre/GraphQL.Server.Ui.Voyager)](https://www.nuget.org/packages/GraphQL.Server.Ui.Voyager) | [![MyGet](https://img.shields.io/myget/graphql-dotnet/vpre/GraphQL.Server.Ui.Voyager?label=myget)](https://www.myget.org/F/graphql-dotnet/api/v3/index.json) |
| GraphQL.Server.Authorization.AspNetCore | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Authorization.AspNetCore)](https://www.nuget.org/packages/GraphQL.Server.Authorization.AspNetCore) | [![Nuget](https://img.shields.io/nuget/vpre/GraphQL.Server.Authorization.AspNetCore)](https://www.nuget.org/packages/GraphQL.Server.Authorization.AspNetCore) | [![MyGet](https://img.shields.io/myget/graphql-dotnet/vpre/GraphQL.Server.Authorization.AspNetCore?label=myget)](https://www.myget.org/F/graphql-dotnet/api/v3/index.json) |

Transport compatible with [Apollo](https://github.com/apollographql/subscriptions-transport-ws) subscription protocol.

## Getting started

> WARNING: The latest stable version 3.4.0 has many known issues that have been fixed in 3.5.0-alphaXXXX versions.
> If errors occur, it is recommended that you first check the behavior on the latest available alpha version before
> reporting a issue. Latest 3.5.0-alphaXXXX versions are **backwards incompatible** with the latest stable 2.4.0
> version of [GraphQL.NET](https://github.com/graphql-dotnet/graphql-dotnet). You can see the changes in public APIs
> using [fuget.org](https://www.fuget.org/packages/GraphQL.Server.Transports.AspNetCore/3.5.0-alpha0046/lib/netstandard2.0/diff/3.4.0/).

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

``` csharp
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
