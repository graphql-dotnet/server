using System.Security.Claims;
using System.Security.Principal;
using GraphQL.Server.Transports.AspNetCore.WebSockets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Transports.AspNetCore;

/// <summary>
/// Configuration options for <see cref="GraphQLHttpMiddleware"/>.
/// </summary>
public class GraphQLHttpMiddlewareOptions
{
    /// <summary>
    /// Enables handling of GET requests.
    /// </summary>
    public bool HandleGet { get; set; } = true;

    /// <summary>
    /// Enables handling of POST requests, including form submissions, JSON-formatted requests and raw query requests.
    /// <para>Supported media types are:</para>
    /// <list type="bullet">
    /// <item>application/x-www-form-urlencoded</item>
    /// <item>multipart/form-data</item>
    /// <item>application/json</item>
    /// <item>application/graphql</item>
    /// </list>
    /// </summary>
    public bool HandlePost { get; set; } = true;

    /// <summary>
    /// Enables handling of WebSockets requests.
    /// <br/><br/>
    /// Requires calling <see cref="WebSocketMiddlewareExtensions.UseWebSockets(IApplicationBuilder)"/>
    /// to initialize the WebSocket pipeline within the ASP.NET Core framework.
    /// </summary>
    public bool HandleWebSockets { get; set; } = true;

    /// <summary>
    /// Enables handling of batched GraphQL requests for POST requests when formatted as JSON.
    /// </summary>
    public bool EnableBatchedRequests { get; set; } = true;

    /// <summary>
    /// Enables parallel execution of batched GraphQL requests.
    /// </summary>
    public bool ExecuteBatchedRequestsInParallel { get; set; } = true;

    /// <summary>
    /// When enabled, GraphQL requests with validation errors
    /// have the HTTP status code set to 400 Bad Request.
    /// GraphQL requests with execution errors are unaffected.
    /// <br/><br/>
    /// Does not apply to batched or WebSocket requests.
    /// </summary>
    public bool ValidationErrorsReturnBadRequest { get; set; } = true;

    /// <summary>
    /// Enables parsing the query string on POST requests.
    /// If enabled, the query string properties override those in the body of the request.
    /// </summary>
    public bool ReadQueryStringOnPost { get; set; } = true;

    /// <summary>
    /// Enables reading variables from the query string.
    /// Variables are interpreted as JSON and deserialized before being
    /// provided to the <see cref="IDocumentExecuter"/>.
    /// </summary>
    public bool ReadVariablesFromQueryString { get; set; } = true;

    /// <summary>
    /// Enables reading extensions from the query string.
    /// Extensions are interpreted as JSON and deserialized before being
    /// provided to the <see cref="IDocumentExecuter"/>.
    /// </summary>
    public bool ReadExtensionsFromQueryString { get; set; } = true;

    /// <summary>
    /// If set, requires that <see cref="IIdentity.IsAuthenticated"/> return <see langword="true"/>
    /// for the user within <see cref="HttpContext.User"/>
    /// prior to executing the GraphQL request or accepting the WebSocket connection.
    /// Technically this property should be named as AuthenticationRequired but for
    /// ASP.NET Core / GraphQL.NET naming and design decisions it was called so. 
    /// </summary>
    public bool AuthorizationRequired { get; set; }

    /// <summary>
    /// Requires that <see cref="ClaimsPrincipal.IsInRole(string)"/> return <see langword="true"/>
    /// for the user within <see cref="HttpContext.User"/>
    /// for at least one role in the list prior to executing the GraphQL request or accepting
    /// the WebSocket connection.  If no roles are specified, authorization is not checked.
    /// </summary>
    public List<string> AuthorizedRoles { get; set; } = new();

    /// <summary>
    /// If set, requires that <see cref="IAuthorizationService.AuthorizeAsync(ClaimsPrincipal, object, string)"/>
    /// return a successful result for the user within <see cref="HttpContext.User"/>
    /// for the specified policy before executing the GraphQL
    /// request or accepting the WebSocket connection.
    /// </summary>
    public string? AuthorizedPolicy { get; set; }

    /// <summary>
    /// Returns an options class for WebSocket connections.
    /// </summary>
    public GraphQLWebSocketOptions WebSockets { get; set; } = new();
}
