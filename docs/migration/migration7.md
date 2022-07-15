# Migrating from v6 to v7

## Major changes and new features

#### GraphQL Middleware

- Configuration simplified to a single line of code
- Single middleware to support GET, POST and WebSocket connections (configurable)
- Media type of 'application/graphql+json' is accepted and returned as recommended by the draft spec (configurable via virtual method)
- Batched requests will execute in parallel within separate service scopes (configurable)
- Authorization rules can be set on endpoints, regardless of schema configuration
- Mutation requests are disallowed over GET connections, as required by the spec
- Support for ASP.NET Core 2.1 and .NET Framework 4.8 has been added
- Middleware includes several configuration options to alter the default behavior without creating a derived class
- Configuration options can be set independently for each configured endpoint
- New OOP design; middleware is easily extended in a derived class
- Removed virtual method `GetCancellationToken`; token is pulled from `HttpContext.RequestAborted`
- `HttpContext.User` is passed to `ExecutionOptions.User` so it can be accessed by validation rules and field resolvers

#### Subscriptions / WebSocket connections

- WebSocket support has been moved into the main project (Transports.AspNetCore) and is included within the middleware above; no separate configuration is necessary
- No 3rd party dependencies required (i.e. DataFlow or System.Reactive)
- WebSocket connections now support both the graphql-ws and graphql-transport-ws protocols
- Tighter adherence to the respective protocol, with configurable timeouts
- Added Server-side keep-alive functionality (configurable)
- New OOP simplified design that can easily be extended to support additional protocols
- If a server-side error occurs via OnError, connected clients are disconnected (configurable)
- Authorization of WebSocket connections via the ConnectionInit message is supported
- The payload from a ConnectionInit message can be considered when building the user context
- Requests execute within their own service scope, and when combined with `ScopedSubscriptionExecutionStrategy`, subscription data events can
  be configured to execute within their own scope as well
- Active subscription connections are terminated when the host is shutting down

#### Authorization rule (new)

- Configuration simplifed to a single line of code
- Support moved into the main project (Transports.AspNetCore); no separate NuGet reference required
- Removed `IClaimsPrincipalAccessor`; `ValidationContext.User` is used to acquire `ClaimsPrincipal` reference
- As a security fix, authorization failure messages do not reveal the authorization requirements to the caller
- Removed `IAuthorizationErrorMessageBuilder`
- Added support for `AuthorizeWithRole(string role)`
- Added support for `Authorize()`
- Added support for `AllowAnonymous()`
- Removed support for authorization checks on all input types
- Authorization checks are skipped for fields or fragments that should be skipped due to an `@skip` or `@include` directive

#### Old authorization rule

- The authorization rule as it existed in GraphQL.NET Server v6 is still present, but marked as `[Obsolete]`.
  `IClaimsPrincipalAccessor` and `IAuthorizationErrorMessageBuilder` are still supported and messages are
  generated in the same manner as in v6.
- Other new features, such as `AuthorizeWithRole` and proper `@skip` support are included.
- These obsolete classes will be removed in v8; please open an issue if you require any of the deprecated features.
- It is important to note that authorization checks on all input types are not supported even with this deprecated rule.

#### MVC projects

- Added an `ExecutionResultActionResult` class for returning GraphQL responses from MVC controller action methods

#### UI middleware

- Support for ASP.NET Core 2.1 and .NET Framework 4.8 has been added
- Supports relative URLs for graphql and subscription endpoints
- Supports fully-qualified URLs for graphql and subscription endpoints
- Supports configuring RequestCredentials for Altair and GraphiQL middleware

#### Sample projects

Added multiple sample projects, as follows:

| Name | Framework | Description |
|-|-|-|
| Authorization | .NET 6 Minimal | Based on the VS template, demonstrates authorization functionality with cookie-based authentication |
| Basic | .NET 6 Minimal | Simplest possible implementation |
| Complex | .NET 3.1 / 5 / 6 | Rename of existing sample server, which demonstrates older Program/Startup files and various configuration options, and multiple UI endpoints |
| Controller | .NET 6 Minimal | MVC implementation; does not include WebSocket support |
| Cors | .NET 6 Minimal | Demonstrates CORS setup |
| EndpointRouting | .NET 6 Minimal | Demonstrates configuring GraphQL through endpoint routing |
| MultipleSchemas | .NET 6 Minimal | Demonstrates configuring multiple schemas within a single server |
| Net48 | .NET Core 2.1 / .NET 4.8 | Demonstrates configuring GraphQL on .NET 4.8 / Core 2.1 |
| Pages | .NET 6 Minimal | Demonstrates configuring GraphQL on top of a Razor Pages template |

#### Testing

- Enhanced testing of all code, reaching approximately 95%+ coverage for Transports.AspNetCore
- Extensive testing of authorization rules

## Migration of middleware

#### General migration notes

1. Remove the call to `AddHttpMiddleware`, and if present, `AddWebSockets`, `AddWebSocketsHttpMiddleware` and `UseGraphQLWebSockets`.
2. Remove the NuGet reference to GraphQL.Server.Transports.Subscriptions.WebSockets, if any.
   Only the GraphQL.Server.Transports.AspNetCore (or the GraphQL.Server.All) NuGet package is necessary.

```csharp
// v6
services.AddGraphQL(b => b
    .AddHttpMiddleware<MySchema>()
    .AddWebSocketsHttpMiddleware<MySchema>()
    .AddWebSockets()
    // other code
);

// v7
services.AddGraphQL(b => b
    // other code
);

// v6
app.UseGraphQL<MySchema>();
app.UseGraphQLWebSockets<MySchema>();
// or
app.UseGraphQL<MySchema>("/graphql");
app.UseGraphQLWebSockets<MySchema>("/graphql");

// v7
app.UseGraphQL<MySchema>();
// or
app.UseGraphQL<MySchema>("/graphql");
// or
app.UseGraphQL();
// or
app.UseGraphQL("/graphql");
```

Note that the call to `app.UseWebSockets()` is part of ASP.NET Core and is still required for WebSocket support.

<details><summary>With derived middleware class</summary><p>

There is no clear example for rewriting derived middleware classes, as most of the virtual methods' signatures have changed.
Be aware that `GraphQLHttpMiddlewareOptions`, or your derived options class, must be passed with the `UseGraphQL` method.

```csharp
class MyMiddleware : GraphQLHttpMiddleware<MySchema>
{
    public MyMiddleware(
        RequestDelegate next,
        IGraphQLTextSerializer serializer,
        IDocumentExecuter<TSchema> documentExecuter,
        IServiceScopeFactory serviceScopeFactory,
        GraphQLHttpMiddlewareOptions options,
        IHostApplicationLifetime hostApplicationLifetime)
        : base(next, serializer, documentExecuter, serviceScopeFactory, options, hostApplicationLifetime)
    {
    }

    // overridden methods here
}

app.UseGraphQL<MyMiddleware>(new GraphQLHttpMiddlewareOptions());
```

</p></details>

<details><summary>With separate subscription endpoint</summary><p>

```csharp
// v6
app.UseGraphQL<MySchema>("/graphql");
app.UseGraphQLWebSockets<MySchema>("/graphqlsubscription");

// v7
app.UseGraphQL<MySchema>("/graphql", o => o.HandleWebSockets = false);
app.UseGraphQL<MySchema>("/graphqlsubscription", o => {
    o.HandleGet = false;
    o.HandlePost = false;
});
```

</p></details>

<details><summary>To retain prior media type of `application/json`</summary><p>

```csharp
class MyMiddleware<TSchema> : GraphQLHttpMiddleware<TSchema>
    where TSchema : ISchema
{
    public MyMiddleware(
        RequestDelegate next,
        IGraphQLTextSerializer serializer,
        IDocumentExecuter<TSchema> documentExecuter,
        IServiceScopeFactory serviceScopeFactory,
        GraphQLHttpMiddlewareOptions options,
        IHostApplicationLifetime hostApplicationLifetime)
        : base(next, serializer, documentExecuter, serviceScopeFactory, options, hostApplicationLifetime)
    {
    }

    protected override string SelectResponseContentType(HttpContext context)
        => "application/json";
}

app.UseGraphQL<MyMiddleware<ISchema>>("/graphql", new GraphQLHttpMiddlewareOptions());
```

</p></details>

<details><summary>If you had code within the `RequestExecutedAsync` protected method</summary><p>

Either override `HandleRequestAsync`, `HandleBatchRequestAsync` and/or `ExecuteRequestAsync`,
or call the builder method `ConfigureExecution` to add code before/after the call to `IDocumentExecuter.ExecuteAsync`.

</p></details>

## Migration of user context builder

If you used the `AddUserContextBuilder` builder method, there are no changes necessary.
For custom implementations of `IUserContextBuilder`, you will need to update the method
signature for `BuildUserContextAsync`.

## Migration of authorization validation rule

| :warning: **Note that authorization rules on input types are ignored in v7** :warning: |
| --- |

If you need `IClaimsPrincipalAccessor`,  `IAuthorizationErrorMessageBuilder`, or the detailed authorization
failure messages provided in v6, then you may use the deprecated authorization rule with no code changes.
Please open an issue within GitHub explaining your need so it may be addressed.

Otherwise, remove the GraphQL.Server.Authorization.AspNetCore NuGet package and make changes as shown below:

```csharp
// v6
services.AddGraphQL(b => b
    .AddGraphQLAuthorization(options => {
        // ASP.NET authorization configuration
    })
    // other code
);

// v7
services.AddGraphQL(b => b
    .AddAuthorizationRule()
    // other code
);
services.AddAuthorization(options => {
    // ASP.NET authorization configuration
});
```

## Migration of UI middleware

The path must now be specified prior to the options class, rather than after.

```csharp
// v6/v7
app.UseGraphQLPlayground();

// v6/v7
app.UseGraphQLPlayground("/");

// v6
app.UseGraphQLPlayground(new PlaygroundOptions(), "/");
// v7
app.UseGraphQLPlayground("/", new PlaygroundOptions());

// v6
app.UseGraphQLPlayground(new PlaygroundOptions());
// v7
app.UseGraphQLPlayground(options: new PlaygroundOptions());
```
