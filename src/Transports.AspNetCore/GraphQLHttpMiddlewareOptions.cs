using MediaTypeHeaderValueMs = Microsoft.Net.Http.Headers.MediaTypeHeaderValue;

namespace GraphQL.Server.Transports.AspNetCore;

/// <summary>
/// Configuration options for <see cref="GraphQLHttpMiddleware"/>.
/// </summary>
public class GraphQLHttpMiddlewareOptions : IAuthorizationOptions
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

    /// <inheritdoc/>
    /// <remarks>
    /// HTTP requests return <c>401 Forbidden</c> when the request is not authenticated.
    /// </remarks>
    public bool AuthorizationRequired { get; set; }

    /// <inheritdoc cref="IAuthorizationOptions.AuthorizedRoles"/>
    /// <remarks>
    /// HTTP requests return <c>403 Forbidden</c> when the user fails the role check.
    /// </remarks>
    public List<string> AuthorizedRoles { get; set; } = new();

    IEnumerable<string> IAuthorizationOptions.AuthorizedRoles => AuthorizedRoles;

    /// <inheritdoc/>
    /// <remarks>
    /// HTTP requests return <c>403 Forbidden</c> when the user fails the policy check.
    /// </remarks>
    public string? AuthorizedPolicy { get; set; }

    /// <summary>
    /// Returns an options class for WebSocket connections.
    /// </summary>
    public GraphQLWebSocketOptions WebSockets { get; set; } = new();

    private MediaTypeHeaderValueMs _defaultResponseContentType = MediaTypeHeaderValueMs.Parse(GraphQLHttpMiddleware.CONTENTTYPE_GRAPHQLRESPONSEJSON);

    /// <summary>
    /// The Content-Type to use for GraphQL responses, if it matches the 'Accept'
    /// HTTP request header. Defaults to "application/graphql-response+json; charset=utf-8".
    /// </summary>
    public MediaTypeHeaderValueMs DefaultResponseContentType
    {
        get => _defaultResponseContentType;
        set
        {
            _defaultResponseContentType = value;
            DefaultResponseContentTypeString = value.ToString();
        }
    }

    internal string DefaultResponseContentTypeString { get; set; } = GraphQLHttpMiddleware.CONTENTTYPE_GRAPHQLRESPONSEJSON;
}
