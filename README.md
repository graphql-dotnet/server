# ASP.NET Core GraphQL Server driven by GraphQL.NET

[![License](https://img.shields.io/github/license/graphql-dotnet/server)](LICENSE.md)
[![codecov](https://codecov.io/gh/graphql-dotnet/server/branch/master/graph/badge.svg?token=ZBcVYq7hz4)](https://codecov.io/gh/graphql-dotnet/server)
[![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Transports.AspNetCore)](https://www.nuget.org/packages/GraphQL.Server.Transports.AspNetCore)
[![Nuget](https://img.shields.io/nuget/v/GraphQL.Server.Transports.AspNetCore)](https://www.nuget.org/packages/GraphQL.Server.Transports.AspNetCore)
[![GitHub Release Date](https://img.shields.io/github/release-date/graphql-dotnet/server?label=released)](https://github.com/graphql-dotnet/server/releases)
[![GitHub commits since latest release (by date)](https://img.shields.io/github/commits-since/graphql-dotnet/server/latest?label=new+commits)](https://github.com/graphql-dotnet/server/commits/master)
[![GitHub contributors](https://img.shields.io/github/contributors/graphql-dotnet/server)](https://github.com/graphql-dotnet/server/graphs/contributors)
![Size](https://img.shields.io/github/repo-size/graphql-dotnet/server)

GraphQL ASP.NET Core server on top of [GraphQL.NET](https://github.com/graphql-dotnet/graphql-dotnet).
HTTP transport compatible with the [GraphQL over HTTP draft specification](https://github.com/graphql/graphql-over-http/blob/main/spec/GraphQLOverHTTP.md).
WebSocket transport compatible with both [subscriptions-transport-ws](https://github.com/apollographql/subscriptions-transport-ws) and
[graphql-ws](https://github.com/enisdenjo/graphql-ws) subscription protocols. The transport format of all messages is supposed to be JSON.

Provides the following packages:

| Package                                              | Downloads                                                                                                                                                                             | Version                                                                                                                                                                              | Description |
|------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-------------|
| GraphQL.Server.All                                   | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.All)](https://www.nuget.org/packages/GraphQL.Server.All)                                                                     | [![Nuget](https://img.shields.io/nuget/v/GraphQL.Server.All)](https://www.nuget.org/packages/GraphQL.Server.All)                                                                     | Includes all the packages below, excluding the legacy authorization package, plus the `GraphQL.DataLoader` and `GraphQL.MemoryCache` packages |
| GraphQL.Server.Authorization.AspNetCore (deprecated) | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Authorization.AspNetCore)](https://www.nuget.org/packages/GraphQL.Server.Authorization.AspNetCore)                           | [![Nuget](https://img.shields.io/nuget/v/GraphQL.Server.Authorization.AspNetCore)](https://www.nuget.org/packages/GraphQL.Server.Authorization.AspNetCore)                           | Provides legacy authorization rule support (deprecated; please use GraphQL.Server.Transports.AspNetCore) |
| GraphQL.Server.Transports.AspNetCore                 | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Transports.AspNetCore)](https://www.nuget.org/packages/GraphQL.Server.Transports.AspNetCore)                                 | [![Nuget](https://img.shields.io/nuget/v/GraphQL.Server.Transports.AspNetCore)](https://www.nuget.org/packages/GraphQL.Server.Transports.AspNetCore)                                 | Provides GraphQL over HTTP/WebSocket server support on top of ASP.NET Core, plus authorization rule support |
| GraphQL.Server.Ui.Altair                             | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Ui.Altair)](https://www.nuget.org/packages/GraphQL.Server.Ui.Altair)                                                         | [![Nuget](https://img.shields.io/nuget/v/GraphQL.Server.Ui.Altair)](https://www.nuget.org/packages/GraphQL.Server.Ui.Altair)                                                         | Provides Altair UI middleware |
| GraphQL.Server.Ui.Playground                         | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Ui.Playground)](https://www.nuget.org/packages/GraphQL.Server.Ui.Playground)                                                 | [![Nuget](https://img.shields.io/nuget/v/GraphQL.Server.Ui.Playground)](https://www.nuget.org/packages/GraphQL.Server.Ui.Playground)                                                 | Provides Playground UI middleware |
| GraphQL.Server.Ui.GraphiQL                           | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Ui.GraphiQL)](https://www.nuget.org/packages/GraphQL.Server.Ui.GraphiQL)                                                     | [![Nuget](https://img.shields.io/nuget/v/GraphQL.Server.Ui.GraphiQL)](https://www.nuget.org/packages/GraphQL.Server.Ui.GraphiQL)                                                     | Provides GraphiQL UI middleware |
| GraphQL.Server.Ui.Voyager                            | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.Server.Ui.Voyager)](https://www.nuget.org/packages/GraphQL.Server.Ui.Voyager)                                                       | [![Nuget](https://img.shields.io/nuget/v/GraphQL.Server.Ui.Voyager)](https://www.nuget.org/packages/GraphQL.Server.Ui.Voyager)                                                       | Provides Voyager UI middleware |

You can install the latest stable versions via [NuGet](https://www.nuget.org/packages/GraphQL.Server.Transports.AspNetCore/).
Also you can get all preview versions from [GitHub Packages](https://github.com/orgs/graphql-dotnet/packages?repo_name=server).
Note that GitHub requires authentication to consume the feed. See more information [here](https://docs.github.com/en/free-pro-team@latest/packages/publishing-and-managing-packages/about-github-packages#authenticating-to-github-packages).

| :warning: When upgrading from prior versions, please remove references to these old packages :warning: |
|-|
| GraphQL.Server.Core |
| GraphQL.Server.Transports.AspNetCore.NewtonsoftJson |
| GraphQL.Server.Transports.AspNetCore.SystemTextJson |
| GraphQL.Server.Transports.Subscriptions.Abstractions |
| GraphQL.Server.Transports.Subscriptions.WebSockets |
| GraphQL.Server.Transports.WebSocktes |

## Description

This package is designed for ASP.NET Core (2.1 through 6.0) to facilitate easy set-up of GraphQL requests
over HTTP.  The code is designed to be used as middleware within the ASP.NET Core pipeline,
serving GET, POST or WebSocket requests.  GET requests process requests from the query string.
POST requests can be in the form of JSON requests, form submissions, or raw GraphQL strings.
WebSocket requests can use the `graphql-ws` or `graphql-transport-ws` WebSocket sub-protocol,
as defined in the [apollographql/subscriptions-transport-ws](https://github.com/apollographql/subscriptions-transport-ws)
and [enisdenjo/graphql-ws](https://github.com/enisdenjo/graphql-ws) repositories, respectively.

The middleware can be configured through the `IApplicationBuilder` or `IEndpointRouteBuilder`
builder interfaces.  In addition, an `ExecutionResultActionResult` class is added for returning
`ExecutionResult` instances directly from a controller action.

Authorization is also supported with the included `AuthorizationValidationRule`.  It will
scan GraphQL documents and validate that the schema and all referenced output graph types, fields of
output graph types, and query arguments meet the specified policy and/or roles held by the
authenticated user within the ASP.NET Core authorization framework.  It does not validate
any policies or roles specified for input graph types, fields of input graph types, or
directives.  It skips validations for fields or fragments that are marked with the `@skip` or
`@include` directives.

See [migration notes](docs/migration/migration7.md) for changes from version 6.x.

## Configuration

### Typical configuration with HTTP middleware

First add either the `GraphQL.Server.All` nuget package or the `GraphQL.Server.Transports.AspNetCore`
nuget package to your application.  Referencing the "all" package will include the UI middleware
packages.  These packages depend on `GraphQL` version 7.0.0 or later.

Then update your `Program.cs` or `Startup.cs` to configure GraphQL, registering the schema
and the serialization engine as a minimum.  Configure WebSockets and GraphQL in the HTTP
pipeline by calling `UseWebSockets` and `UseGraphQL` at the appropriate point.
Finally, you may also include some UI middleware for easy testing of your GraphQL endpoint
by calling `UseGraphQLVoyager` or a similar method at the appropriate point.

Below is a complete sample of a .NET 6 console app that hosts a GraphQL endpoint at
`http://localhost:5000/graphql`:

#### Project file

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="GraphQL.Server.All" Version="7.0.0" />
  </ItemGroup>

</Project>
```

#### Program.cs file

```csharp
using GraphQL;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGraphQL(b => b
    .AddAutoSchema<Query>()  // schema
    .AddSystemTextJson());   // serializer

var app = builder.Build();
app.UseDeveloperExceptionPage();
app.UseWebSockets();
app.UseGraphQL("/graphql");            // url to host GraphQL endpoint
app.UseGraphQLPlayground(
    "/",                               // url to host Playground at
    new GraphQL.Server.Ui.Playground.PlaygroundOptions
    {
        GraphQLEndPoint = "/graphql",         // url of GraphQL endpoint
        SubscriptionsEndPoint = "/graphql",   // url of GraphQL endpoint
    });
await app.RunAsync();
```

#### Schema

```csharp
public class Query
{
    public static string Hero() => "Luke Skywalker";
}
```

#### Sample request url

```
http://localhost:5000/graphql?query={hero}
```

#### Sample response

```json
{"data":{"hero":"Luke Skywalker"}}
```

### Configuration with endpoint routing

To use endpoint routing, call `MapGraphQL` from inside the endpoint configuration
builder rather than `UseGraphQL` on the application builder.  See below for the
sample of the application builder code:

```csharp
var app = builder.Build();
app.UseDeveloperExceptionPage();
app.UseWebSockets();
app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapGraphQL("graphql");
    endpoints.MapGraphQLVoyager("ui/voyager");
});
await app.RunAsync();
```

### Configuration with a MVC controller

Although not recommended, you may set up a controller action to execute GraphQL
requests.  You will not need `UseGraphQL` or `MapGraphQL` in the application
startup.  You may use `GraphQLExecutionActionResult` to let the middleware
handle the entire parsing and execution of the request, including subscription
requests over WebSocket connections, or you can execute the request yourself,
only using `ExecutionResultActionResult` to serialize the result.

You can also reference the UI projects to display a GraphQL user interface as shown below.

#### Using `GraphQLExecutionActionResult`

```csharp
public class HomeController : Controller
{
    public IActionResult Index()
        => new GraphiQLActionResult(opts =>
        {
            opts.GraphQLEndPoint = "/Home/graphql";
            opts.SubscriptionsEndPoint = "/Home/graphql";
        });

    [HttpGet]
    [HttpPost]
    [ActionName("graphql")]
    public IActionResult GraphQL()
        => new GraphQLExecutionActionResult();
}
```

#### Using `ExecutionResultActionResult`

Note: this is very simplified; a much more complete sample can be found
in the `Samples.Controller` project within this repository.

```csharp
public class HomeController : Controller
{
    private readonly IDocumentExecuter _documentExecuter;

    public TestController(IDocumentExecuter<ISchema> documentExecuter)
    {
        _documentExecuter = documentExecuter;
    }

    [HttpGet]
    public async Task<IActionResult> GraphQL(string query)
    {
        var result = await _documentExecuter.ExecuteAsync(new()
        {
            Query = query,
            RequestServices = HttpContext.RequestServices,
            CancellationToken = HttpContext.RequestAborted,
        });
        return new ExecutionResultActionResult(result);
    }
}
```

### Configuration with Azure Functions

This project also supports hosting GraphQL endpoints within Azure Functions.
You will need to complete the following steps:

1. Configure the Azure Function to use Dependency Injection:
   See https://learn.microsoft.com/en-us/azure/azure-functions/functions-dotnet-dependency-injection
   for details.

2. Configure GraphQL via `builder.Services.AddGraphQL()` the same as you would in a typical
   ASP.NET Core application.

3. Add an HTTP function that returns an appropriate `ActionResult`:

```csharp
[FunctionName("GraphQL")]
public static IActionResult RunGraphQL(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post"] HttpRequest req)
{
    return new GraphQLExecutionActionResult();
}
```

5. Optionally, add a UI package to the project and configure it:

```csharp
[FunctionName("Playground")]
public static IActionResult RunGraphQL(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get"] HttpRequest req)
{
    return new PlaygroundActionResult(opts => opts.GraphQLEndPoint = "/api/graphql");
}
```

Middleware can be configured by passing a configuration delegate to `new GraphQLExecutionActionResult()`.
Multiple schemas are supported by the use of `GraphQLExecutionActionResult<TSchema>()`.
It is not possible to configure subscription support, as Azure Functions do not support WebSockets
since it is a serverless environment.

See the `Samples.AzureFunctions` project for a complete sample based on the
.NET template for Azure Functions.

Please note that the GraphQL schema needs to be initialized for every call through
Azure Functions, since it is a serverless environment.  This is done automatically
but will come at a performance cost.  If you are using a schema that is expensive
to initialize, you may want to consider using a different hosting environment.

### User context configuration

To set the user context to be used during the execution of GraphQL requests,
call `AddUserContextBuilder` during the GraphQL service setup to set a delegate
which will be called when the user context is built.  Alternatively, you can
register an `IUserContextBuilder` implementation to do the same.

#### Program.cs / Startup.cs

```csharp
builder.Services.AddGraphQL(b => b
    .AddAutoSchema<Query>()
    .AddSystemTextJson()
    .AddUserContextBuilder(httpContext => new MyUserContext(httpContext));
```

#### MyUserContext.cs

```csharp
public class MyUserContext : Dictionary<string, object?>
{
    public ClaimsPrincipal User { get; }

    public MyUserContext(HttpContext context)
    {
        User = context.User;
    }
}
```

### Authorization configuration

You can configure authorization for all GraphQL requests, or for individual
graphs, fields and query arguments within your schema.  Both can be used
if desired.

Be sure to call `app.UseAuthentication()` and `app.UseAuthorization()` prior
to the call to `app.UseGraphQL()`.  For example:

```csharp
app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets();
app.UseGraphQL("/graphql");
```

#### Endpoint authorization (which would include introspection requests)

Endpoint authorization will check authorization requirements are met for the entire
GraphQL endpoint, including introspection requests.  These checks occur prior to parsing,
validating or executing the document.

When calling `UseGraphQL`, specify options as necessary to configure authorization as required.

```csharp
app.UseGraphQL("/graphql", config =>
{
    // require that the user be authenticated
    config.AuthorizationRequired = true;

    // require that the user be a member of at least one role listed
    config.AuthorizedRoles.Add("MyRole");
    config.AuthorizedRoles.Add("MyAlternateRole");

    // require that the user pass a specific authorization policy
    config.AuthorizedPolicy = "MyPolicy";
});
```

#### For individual graph types, fields and query arguments

To configure the ASP.NET Core authorization validation rule for GraphQL, add the corresponding
validation rule during GraphQL configuration, typically by calling `.AddAuthorizationRule()`
as shown below:

```csharp
builder.Services.AddGraphQL(b => b
    .AddAutoSchema<Query>()
    .AddSystemTextJson()
    .AddAuthorizationRule());
```

Both roles and policies are supported for output graph types, fields on output graph types,
and query arguments.  If multiple policies are specified, all must match; if multiple roles
are specified, any one role must match.  You may also use `.Authorize()` and/or the
`[Authorize]` attribute to validate that the user has authenticated.  You may also use
`.AllowAnonymous()` and/or `[AllowAnonymous]` to allow fields to be returned to
unauthenticated users within an graph that has an authorization requirement defined.

Please note that authorization rules do not apply to values returned within introspection requests,
potentially leaking information about protected areas of the schema to unauthenticated users.
You may use the `ISchemaFilter` to restrict what information is returned from introspection
requests, but it will apply to both authenticated and unauthenticated users alike.

Introspection requests are allowed unless the schema has an authorization requirement set on it.
The `@skip` and `@include` directives are honored, skipping authorization checks for fields
or fragments skipped by `@skip` or `@include`.

Please note that if you use interfaces, validation might be executed against the graph field
or the interface field, depending on the structure of the query.  For instance:

```graphql
{
  cat {
    # validates against Cat.Name
    name

    # validates against Animal.Name
    ... on Animal {
      name
    }
  }
}
```

Similarly for unions, validation occurs on the exact type that is queried.  Be sure to carefully
consider placement of authorization rules when using interfaces and unions, especially when some
fields are marked with `AllowAnonymous`.

| :warning: Note that authorization rules are ignored for input types and fields of input types :warning: |
|-|

#### Custom authentication configuration for GET/POST requests

To provide custom authentication code, bypassing ASP.NET Core's authentication, derive from the
`GraphQLHttpMiddleware<T>` class and override `HandleAuthorizeAsync`, setting `HttpContext.User`
to an appropriate `ClaimsPrincipal` instance.

See 'Customizing middleware behavior' below for an example of deriving from `GraphQLHttpMiddleware`.

#### Authentication for WebSocket requests

Since WebSocket requests from browsers cannot typically carry a HTTP Authorization header, you
will need to authorize requests via the `ConnectionInit` WebSocket message or carry the authorization
token within the URL.  Below is a sample of the former:

```cs
builder.Services.AddGraphQL(b => b
    .AddAutoSchema<Query>()
    .AddSystemTextJson()
    .AddAuthorizationRule()  // not required for endpoint authorization
    .AddWebSocketAuthentication<MyAuthService>());

app.UseGraphQL("/graphql", config =>
{
    // require that the user be authenticated
    config.AuthorizationRequired = true;
});

class MyAuthService : IWebSocketAuthenticationService
{
    private readonly IGraphQLSerializer _serializer;

    public MyAuthService(IGraphQLSerializer serializer)
    {
        _serializer = serializer;
    }

    public async ValueTask<bool> AuthenticateAsync(IWebSocketConnection connection, OperationMessage operationMessage)
    {
        // read payload of ConnectionInit message and look for an "Authorization" entry that starts with "Bearer "
        var payload = _serializer.ReadNode<Inputs>(operationMessage.Payload);
        if ((payload?.TryGetValue("Authorization", out var value) ?? false) && value is string valueString)
        {
            var user = ParseToken(valueString);
            if (user != null)
            {
                // set user and indicate authentication was successful
                connection.HttpContext.User = user;
                return true;
            }
        }
        return false; // authentication failed
    }

    private ClaimsPrincipal? ParseToken(string authorizationHeaderValue)
    {
        // parse header value and return user, or null if unable
    }
}
```

To authorize based on information within the query string, it is recommended to
derive from `GraphQLHttpMiddleware<T>` and override `InvokeAsync`, setting
`HttpContext.User` based on the query string parameters, and then calling `base.InvokeAsync`.
Alternatively you may override `HandleAuthorizeAsync` which will execute for GET/POST requests,
and `HandleAuthorizeWebSocketConnectionAsync` for WebSocket requests.
Note that `InvokeAsync` will execute even if the protocol is disabled in the options via
disabling `HandleGet` or similar; `HandleAuthorizeAsync` and `HandleAuthorizeWebSocketConnectionAsync`
will not.

### UI configuration

There are four UI middleware projects included; Altair, GraphiQL, Playground and Voyager.
See review the following methods for configuration options within each of the 4 respective
NuGet packages:

```csharp
app.UseGraphQLAltair();
app.UseGraphQLGraphiQL();
app.UseGraphQLPlayground();
app.UseGraphQLVoyager();

// or

endpoints.MapGraphQLAltair();
endpoints.MapGraphQLGraphiQL();
endpoints.MapGraphQLPlayground();
endpoints.MapGraphQLVoyager();
```

### CORS configuration

ASP.NET Core supports CORS requests independently of GraphQL, including CORS pre-flight
requests.  To configure your application for CORS requests, add `AddCors()` and `UseCors()`
into the application pipeline.

```csharp
builder.Services.AddCors();

app.UseCors(policy => {
    // configure default policy here
});
```

To configure GraphQL to use a named CORS policy, configure the application to use endpoint routing
and call `RequireCors()` on the endpoint configuration builder.

```csharp
// ...
builder.Services.AddRouting();
builder.Services.AddCors(builder =>
{
    // configure named and/or default policies here
});

var app = builder.Build();
app.UseDeveloperExceptionPage();
app.UseWebSockets();
app.UseRouting();
app.UseCors();
app.UseEndpoints(endpoints =>
{
    // configure the graphql endpoint with the specified CORS policy
    endpoints.MapGraphQL()
        .RequireCors("MyCorsPolicy");
});
await app.RunAsync();
```

### Response compression

ASP.NET Core supports response compression independently of GraphQL, with brotli and gzip
support automatically based on the compression formats listed as supported in the request headers.
To configure your application for response compression, configure your Program/Startup file as
follows:

```csharp
// add and configure the service
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true; // may lead to CRIME and BREACH attacks
    options.MimeTypes = new[] { "application/json", "application/graphql-response+json" };
})

// place this first/early in the pipeline
app.UseResponseCompression();
```

In order to compress GraphQL responses, the `application/graphql-response+json` content type must be
added to the `MimeTypes` option.  You may choose to enable other content types as well.

Please note that enabling response compression over HTTPS can lead to CRIME and BREACH
attacks.  These side-channel attacks typically affects sites that rely on cookies for
authentication.  Please read [this](https://docs.microsoft.com/en-us/aspnet/core/performance/response-compression?view=aspnetcore-6.0)
and [this](http://www.breachattack.com/#howitworks) for more details.

### ASP.NET Core 2.1 / .NET Framework 4.8

You may choose to use the .NET Core 2.1 runtime or the .NET Framework 4.8 runtime.
This library has been tested with .NET Core 2.1 and .NET Framework 4.8.

The only additional requirement is that you must add this code in your `Startup.cs` file:

```csharp
services.AddHostApplicationLifetime();
```

Besides that requirement, all features are supported in exactly the same manner as
when using ASP.NET Core 3.1+.  You may find differences in the ASP.NET Core runtime,
such as CORS implementation differences, which are outside the scope of this project.

Please note that a serializer reference is not included for these projects within
`GraphQL.Server.Transports.AspNetCore`; you will need to reference either
`GraphQL.NewtonsoftJson` or `GraphQL.SystemTextJson`, or reference
`GraphQL.Server.All` which includes `GraphQL.NewtonsoftJson` for ASP.NET Core 2.1 projects.
This is because `Newtonsoft.Json` is the default serializer for ASP.NET Core 2.1
rather `System.Text.Json`.  When using `GraphQL.NewtonsoftJson`, you will need to call
`AddNewtonsoftJson()` rather than `AddSystemTextJson()` while configuring GraphQL.NET.

<details><summary>Microsoft support policy</summary><p>

Please note that .NET Core 2.1 is currently out of support by Microsoft.
.NET Framework 4.8 is supported, and ASP.NET Core 2.1 is supported when run on
.NET Framework 4.8.  Please see these links for more information:

- https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-framework
- https://dotnet.microsoft.com/en-us/platform/support/policy/aspnetcore-2.1

</p></details>

## Advanced configuration

For more advanced configurations, see the overloads and configuration options
available for the various builder methods, listed below.  Methods and properties
contain XML comments to provide assistance while coding with your IDE.

| Builder interface       | Method                  | Description |
|-------------------------|-------------------------|-------------|
| `IGraphQLBuilder`       | `AddUserContextBuilder` | Sets up a delegate to create the UserContext for each GraphQL request. |
| `IApplicationBuilder`   | `UseGraphQL`            | Adds the GraphQL middleware to the HTTP request pipeline. |
| `IEndpointRouteBuilder` | `MapGraphQL`            | Adds the GraphQL middleware to the HTTP request pipeline. |

A number of the methods contain optional parameters or configuration delegates to
allow further customization.  Please review the overloads of each method to determine
which options are available.  In addition, many methods have more descriptive XML
comments than shown above.

### Configuration options

Below are descriptions of the options available when registering the HTTP middleware.
Note that the HTTP middleware options are configured via the `UseGraphQL` or `MapGraphQL`
methods allowing for different options for each configured endpoint.

#### GraphQLHttpMiddlewareOptions

| Property                           | Description     | Default value |
|------------------------------------|-----------------|---------------|
| `AuthorizationRequired`            | Requires `HttpContext.User` to represent an authenticated user. | False |
| `AuthorizedPolicy`                 | If set, requires `HttpContext.User` to pass authorization of the specified policy. | |
| `AuthorizedRoles`                  | If set, requires `HttpContext.User` to be a member of any one of a list of roles. | |
| `DefaultResponseContentType`       | Sets the default response content type used within responses. | `application/graphql-response+json; charset=utf-8` |
| `EnableBatchedRequests`            | Enables handling of batched GraphQL requests for POST requests when formatted as JSON. | True |
| `ExecuteBatchedRequestsInParallel` | Enables parallel execution of batched GraphQL requests. | True |
| `HandleGet`                        | Enables handling of GET requests. | True |
| `HandlePost`                       | Enables handling of POST requests. | True |
| `HandleWebSockets`                 | Enables handling of WebSockets requests. | True |
| `ReadExtensionsFromQueryString`    | Enables reading extensions from the query string. | True |
| `ReadQueryStringOnPost`            | Enables parsing the query string on POST requests. | True |
| `ReadVariablesFromQueryString`     | Enables reading variables from the query string. | True |
| `ValidationErrorsReturnBadRequest` | When enabled, GraphQL requests with validation errors have the HTTP status code set to 400 Bad Request. | True |
| `WebSockets`                       | Returns a set of configuration properties for WebSocket connections. | |

#### GraphQLWebSocketOptions

| Property                    | Description          | Default value |
|-----------------------------|----------------------|---------------|
| `ConnectionInitWaitTimeout` | The amount of time to wait for a GraphQL initialization packet before the connection is closed. | 10 seconds |
| `KeepAliveTimeout`          | The amount of time to wait between sending keep-alive packets. | disabled |
| `DisconnectionTimeout`      | The amount of time to wait to attempt a graceful teardown of the WebSockets protocol. | 10 seconds |
| `DisconnectAfterErrorEvent` | Disconnects a subscription from the client if the subscription source dispatches an `OnError` event. | True |
| `DisconnectAfterAnyError`   | Disconnects a subscription from the client if there are any GraphQL errors during a subscription. | False |

### Multi-schema configuration

You may use the generic versions of the various builder methods to map a URL to a particular schema.

```csharp
var app = builder.Build();
app.UseDeveloperExceptionPage();
app.UseGraphQL<DogSchema>("/dogs/graphql");
app.UseGraphQL<CatSchema>("/cats/graphql");
await app.RunAsync();
```

### Different global authorization settings for different transports (GET/POST/WebSockets)

You may register the same endpoint multiple times if necessary to configure GET connections
with certain authorization options, and POST connections with other authorization options.

```csharp
var app = builder.Build();
app.UseDeveloperExceptionPage();
app.UseGraphQL("/graphql", options =>
{
    options.HandleGet = true;
    options.HandlePost = false;
    options.HandleWebSockets = false;
    options.AuthorizationRequired = false;
});
app.UseGraphQL("/graphql", options =>
{
    options.HandleGet = false;
    options.HandlePost = true;
    options.HandleWebSockets = true;
    options.AuthorizationRequired = true;   // require authentication for POST/WebSocket connections
});
await app.RunAsync();
```

Since POST and WebSockets can be used for query requests, it is recommended not to do the above,
but instead add the authorization validation rule and add authorization metadata on the Mutation
and Subscription portions of your schema, as shown below:

```csharp
builder.Services.AddGraphQL(b => b
    .AddSchema<MySchema>()
    .AddSystemTextJson()
    .AddAuthorizationRule()); // add authorization validation rule

var app = builder.Build();
app.UseDeveloperExceptionPage();
app.UseGraphQL();
await app.RunAsync();

// demonstration of code-first schema; also possible with schema-first or type-first schemas
public class MySchema : Schema
{
    public MySchema(IServiceProvider provider, MyQuery query, MyMutation mutation) : base(provider)
    {
        Query = query;
        Mutation = mutation;

        mutation.Authorize(); // require authorization for any mutation request
    }
}
```

### Customizing middleware behavior

GET/POST requests are handled directly by the `GraphQLHttpMiddleware`.
For WebSocket requests an `WebSocketConnection` instance is created to dispatch incoming
messages and send outgoing messages.  Depending on the WebSocket sub-protocols supported by the
client, the proper implementation of `IOperationMessageProcessor` is created to act as a
state machine, processing incoming messages and sending outgoing messages through the
`WebSocketConnection` instance.

#### GraphQLHttpMiddleware

The base middleware functionality is contained within `GraphQLHttpMiddleware`, with code
to perform execution of GraphQL requests in the derived class `GraphQLHttpMiddleware<TSchema>`.
The classes are organized as follows:

- `InvokeAsync` is the entry point to the middleware.  For WebSocket connection requests,
  execution is immediately passed to `HandleWebSocketAsync`.
- Methods that start with `Handle` are passed the `HttpContext` and `RequestDelegate`
  instance, and may handle the request or pass execution to the `RequestDelegate` thereby
  skipping this execution handler.  This includes methods to handle execution of single or
  batch queries or returning error conditions.
- Methods that start with `Write` are for writing responses to the output stream.
- Methods that start with `Execute` are for executing GraphQL requests.

A list of methods are as follows:

| Method                      | Description |
|-----------------------------|-------------|
| `InvokeAsync`               | Entry point of the middleware |
| `HandleRequestAsync`        | Handles a single GraphQL request. |
| `HandleBatchRequestAsync`   | Handles a batched GraphQL request. |
| `HandleWebSocketAsync`      | Handles a WebSocket connection request. |
| `BuildUserContextAsync`     | Builds the user context based on a `HttpContext`. |
| `ExecuteRequestAsync`       | Executes a GraphQL request. |
| `ExecuteScopedRequestAsync` | Executes a GraphQL request with a scoped service provider. |
| `SelectResponseContentType` | Selects a content-type header for the JSON-formatted GraphQL response. |
| `WriteErrorResponseAsync`   | Writes the specified error message as a JSON-formatted GraphQL response, with the specified HTTP status code. |
| `WriteJsonResponseAsync`    | Writes the specified object (usually a GraphQL response) as JSON to the HTTP response stream. |

| Error handling method                         | Description |
|-----------------------------------------------|-------------|
| `HandleBatchedRequestsNotSupportedAsync`      | Writes a '400 Batched requests are not supported.' message to the output. |
| `HandleContentTypeCouldNotBeParsedErrorAsync` | Writes a '415 Invalid Content-Type header: could not be parsed.' message to the output. |
| `HandleDeserializationErrorAsync`             | Writes a '400 JSON body text could not be parsed.' message to the output. |
| `HandleInvalidContentTypeErrorAsync`          | Writes a '415 Invalid Content-Type header: non-supported type.' message to the output. |
| `HandleInvalidHttpMethodErrorAsync`           | Indicates that an unsupported HTTP method was requested. Executes the next delegate in the chain by default. |
| `HandleWebSocketSubProtocolNotSupportedAsync` | Writes a '400 Invalid WebSocket sub-protocol.' message to the output. |

Below is a sample of custom middleware to change the response content type to `application/json`,
regardless of the value of the HTTP 'Accept' header or default value set in the options:

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

Be sure to derive from `GraphQLHttpMiddleware<TSchema>` rather than `GraphQLHttpMiddleware`
as shown above for multi-schema compatibility.

#### WebSocket handler classes

The WebSocket handling code is organized as follows:

| Interface / Class                | Description |
|----------------------------------|-------------|
| `IWebSocketConnection`           | Provides methods to send a message to a client or close the connection. |
| `IOperationMessageProcessor`     | Handles incoming messages from the client. |
| `GraphQLWebSocketOptions`        | Provides configuration options for WebSocket connections. |
| `WebSocketConnection`            | Standard implementation of a message pump for `OperationMessage` messages across a WebSockets connection.  Implements `IWebSocketConnection` and delivers messages to a specified `IOperationMessageProcessor`. |
| `BaseSubscriptionServer`         | Abstract implementation of `IOperationMessageProcessor`, a message handler for `OperationMessage` messages.  Provides base functionality for managing subscriptions and requests. |
| `GraphQLWs.SubscriptionServer`   | Implementation of `IOperationMessageProcessor` for the `graphql-transport-ws` sub-protocol. |
| `SubscriptionsTransportWs.SubscriptionServer` | Implementation of `IOperationMessageProcessor` for the `graphql-ws` sub-protocol. |
| `IWebSocketAuthorizationService` | Allows authorization of GraphQL requests for WebSocket connections. |

Typically if you wish to change functionality or support another sub-protocol
you will need to perform the following:

1. Derive from either `SubscriptionServer` class, modifying functionality as needed, or to support
   a new protocol, derive from `BaseSubscriptionServer`.
2. Derive from `GraphQLHttpMiddleware<T>` and override `CreateMessageProcessor` and/or
   `SupportedWebSocketSubProtocols` as needed.
3. Change the `app.AddGraphQL()` call to use your custom middleware, being sure to include an instance
   of the options class that your middleware requires (typically `GraphQLHttpMiddlewareOptions`).

There exists a few additional classes to support the above.  Please refer to the source code
of `GraphQLWs.SubscriptionServer` if you are attempting to add support for another protocol.

## Additional notes / FAQ

### Service scope

By default, a dependency injection service scope is created for each GraphQL execution
in cases where it is possible that multiple GraphQL requests may be executing within the
same service scope:

1. A batched GraphQL request is executed.
2. A GraphQL request over a WebSocket connection is executed.

However, field resolvers for child fields of subscription nodes will not by default execute
with a service scope.  Rather, the `context.RequestServices` property will contain a reference
to a disposed service scope that cannot be used.

To solve this issue, please configure the scoped subscription execution strategy from the
GraphQL.MicrosoftDI package as follows:

```csharp
services.AddGraphQL(b => b
    .AddAutoSchema<Query>()
    .AddSystemTextJson()
    // configure queries for serial execution (optional)
    .AddExecutionStrategy<SerialExecutionStrategy>(OperationType.Query)
    // configure subscription field resolvers for scoped serial execution (parallel is optional)
    .AddScopedSubscriptionExecutionStrategy());
```

For single GET / POST requests, the service scope from the underlying HTTP context is used.

### User context builder

The user context builder interface is executed only once, within the dependency injection
service scope of the original HTTP request.  For batched requests, the same user context
instance is passed to each GraphQL execution.  For WebSocket requests, the same user
context instance is passed to each GraphQL subscription and data event resolver execution.

As such, do not create objects within the user context that rely on having the same
dependency injection service scope as the field resolvers.  Since WebSocket connections
are long-lived, using scoped services within a user context builder will result in those
scoped services having a matching long lifetime.  You may wish to alleviate this by
creating a service scope temporarily within your user context builder.

### Mutations within GET requests

For security reasons and pursuant to current recommendations, mutation GraphQL requests
are rejected over HTTP GET connections.  Derive from `GraphQLHttpMiddleware<T>` and override
`ExecuteRequestAsync` to prevent injection of the validation rules that enforce this behavior.

As would be expected, subscription requests are only allowed over WebSocket channels.

### Excessive `OperationCanceledException`s

When hosting a WebSockets endpoint, it may be common for clients to simply disconnect rather
than gracefully terminating the connection — most specifically when the client is a web browser.
If you log exceptions, you may notice an `OperationCanceledException` logged any time this occurs.

In some scenarios you may wish to log these exceptions — for instance, when the GraphQL endpoint is
used in server-to-server communications — but if you wish to ignore these exceptions, simply call
`app.UseIgnoreDisconnections();` immediately after any exception handling or logging configuration calls.
This will consume any `OperationCanceledException`s when `HttpContext.RequestAborted` is signaled — for
a WebSocket request or any other request.

```csharp
var app = builder.Build();
app.UseDeveloperExceptionPage();
app.UseIgnoreDisconnections();
app.UseWebSockets();
app.UseGraphQL();
```

### File uploading/downloading

A common question is how to upload or download files attached to GraphQL data.
For instance, storing and retrieving photographs attached to product data.

One common technique is to encode the data as Base64 and transmitting as a custom
GraphQL scalar (encoded as a string value within the JSON transport).
This may not be ideal, but works well for smaller files.  It can also couple with
response compression (details listed above) to reduce the impact of the Base64
encoding.

Another technique is to get or store the data out-of-band.  For responses, this can
be as simple as a Uri pointing to a location to retrieve the data, especially if
the data are photographs used in a SPA client application.  This may have additional
security complications, especially when used with JWT bearer authentication.
This answer often works well for GraphQL queries, but may not be desired during
uploads (mutations).

An option for uploading is to upload file data alongside a mutation with the `multipart/form-data`
content type.  Please see [Issue 307](https://github.com/graphql-dotnet/server/issues/307) and
[FileUploadTests.cs](https://github.com/graphql-dotnet/server/blob/master/tests/Transports.AspNetCore.Tests/Middleware/FileUploadTests.cs)
for discussion and demonstration of this capability.

## Samples

The following samples are provided to show how to integrate this project with various
typical ASP.NET Core scenarios.

| Name            | Framework                | Description |
|-----------------|--------------------------|-------------|
| Authorization   | .NET 6 Minimal           | Based on the VS template, demonstrates authorization functionality with cookie-based authentication |
| Basic           | .NET 6 Minimal           | Demonstrates simplest possible implementation |
| Complex         | .NET 3.1 / 5 / 6         | Demonstrates older Program/Startup files and various configuration options, and multiple UI endpoints |
| Controller      | .NET 6 Minimal           | MVC implementation; does not include WebSocket support |
| Cors            | .NET 6 Minimal           | Demonstrates configuring a GraphQL endpoint to use a specified CORS policy |
| EndpointRouting | .NET 6 Minimal           | Demonstrates configuring GraphQL through endpoint routing |
| Jwt             | .NET 6 Minimal           | Demonstrates authenticating GraphQL requests with a JWT bearer token over HTTP POST and WebSocket connections |
| MultipleSchemas | .NET 6 Minimal           | Demonstrates configuring multiple schemas within a single server |
| Net48           | .NET Core 2.1 / .NET 4.8 | Demonstrates configuring GraphQL on .NET 4.8 / Core 2.1 |
| Pages           | .NET 6 Minimal           | Demonstrates configuring GraphQL on top of a Razor Pages template |

Most of the above samples rely on a sample "Chat" schema.
Below are some basic requests you can use to test the schema:

### Queries

#### Return number of messages

```graphql
{
  count
}
```

#### Return last message

```graphql
{
  lastMessage {
    id
    message
    from
    sent
  }
}
```

### Mutations

#### Add a message

```graphql
mutation {
  addMessage(message: { message: "Hello", from: "John Doe" }) {
    id
  }
}
```

#### Clear all messages

```graphql
mutation {
  clearMessages
}
```

### Subscriptions

```graphql
subscription {
  newMessages {
    id
    message
    from
    sent
  }
}
```

## Contributors

This project exists thanks to all the people who contribute. 
<a href="https://github.com/graphql-dotnet/server/graphs/contributors"><img src="https://contributors-img.web.app/image?repo=graphql-dotnet/server" /></a>

PRs are welcome! Looking for something to work on? The list of [open issues](https://github.com/graphql-dotnet/server/issues)
is a great place to start. You can help the project by simply responding to some of the [asked questions](https://github.com/graphql-dotnet/server/issues?q=is%3Aissue+is%3Aopen+label%3Aquestion).
