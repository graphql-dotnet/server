using System.Net;
using System.Net.Http.Headers;
using GraphQL.Server.Transports.AspNetCore.Errors;
using GraphQL.Transport;
using GraphQL.Types;
using GraphQL.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server.Transports.AspNetCore;

/// <inheritdoc/>
/// <typeparam name="TSchema">
/// Type of GraphQL schema that is used to validate and process requests.
/// This may be a typed schema as well as <see cref="ISchema"/>.  In both cases registered schemas will be pulled from
/// the dependency injection framework.  Note that when specifying <see cref="ISchema"/> the first schema registered via
/// <see cref="GraphQLBuilderExtensions.AddSchema{TSchema}(DI.IGraphQLBuilder, DI.ServiceLifetime)">AddSchema</see>
/// will be pulled (the "default" schema).
/// </typeparam>
public class GraphQLHttpMiddleware<TSchema> : GraphQLHttpMiddleware
    where TSchema : ISchema
{
    private readonly IDocumentExecuter _documentExecuter;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IEnumerable<IValidationRule> _getValidationRules;
    private readonly IEnumerable<IValidationRule> _getCachedDocumentValidationRules;
    private readonly IEnumerable<IValidationRule> _postValidationRules;
    private readonly IEnumerable<IValidationRule> _postCachedDocumentValidationRules;

    // important: when using convention-based ASP.NET Core middleware, the first constructor is always used

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public GraphQLHttpMiddleware(
        RequestDelegate next,
        IGraphQLTextSerializer serializer,
        IDocumentExecuter<TSchema> documentExecuter,
        IServiceScopeFactory serviceScopeFactory,
        GraphQLHttpMiddlewareOptions options)
        : base(next, serializer, options)
    {
        _documentExecuter = documentExecuter ?? throw new ArgumentNullException(nameof(documentExecuter));
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        var getRule = new HttpGetValidationRule();
        _getValidationRules = DocumentValidator.CoreRules.Append(getRule).ToArray();
        _getCachedDocumentValidationRules = new[] { getRule };
        var postRule = new HttpPostValidationRule();
        _postValidationRules = DocumentValidator.CoreRules.Append(postRule).ToArray();
        _postCachedDocumentValidationRules = new[] { postRule };
    }

    /************* WebSocket support ***********

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public GraphQLHttpMiddleware(
        RequestDelegate next,
        IGraphQLTextSerializer serializer,
        IDocumentExecuter<TSchema> documentExecuter,
        IServiceScopeFactory serviceScopeFactory,
        GraphQLHttpMiddlewareOptions options,
        IServiceProvider provider,
        IHostApplicationLifetime hostApplicationLifetime)
        : this(next, serializer, documentExecuter, serviceScopeFactory, options,
              CreateWebSocketHandlers(serializer, documentExecuter, serviceScopeFactory, provider, hostApplicationLifetime, options))
    {
    }

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    protected GraphQLHttpMiddleware(
        RequestDelegate next,
        IGraphQLTextSerializer serializer,
        IDocumentExecuter<TSchema> documentExecuter,
        IServiceScopeFactory serviceScopeFactory,
        GraphQLHttpMiddlewareOptions options,
        IEnumerable<IWebSocketHandler<TSchema>>? webSocketHandlers = null)
        : base(next, serializer, options, webSocketHandlers)
    {
        _documentExecuter = documentExecuter ?? throw new ArgumentNullException(nameof(documentExecuter));
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        var getRule = new HttpGetValidationRule();
        _getValidationRules = DocumentValidator.CoreRules.Append(getRule).ToArray();
        _getCachedDocumentValidationRules = new[] { getRule };
        var postRule = new HttpPostValidationRule();
        _postValidationRules = DocumentValidator.CoreRules.Append(postRule).ToArray();
        _postCachedDocumentValidationRules = new[] { postRule };
    }

    private static IEnumerable<IWebSocketHandler<TSchema>> CreateWebSocketHandlers(
        IGraphQLSerializer serializer,
        IDocumentExecuter<TSchema> documentExecuter,
        IServiceScopeFactory serviceScopeFactory,
        IServiceProvider provider,
        IHostApplicationLifetime hostApplicationLifetime,
        GraphQLHttpMiddlewareOptions options)
    {
        if (hostApplicationLifetime == null)
            throw new ArgumentNullException(nameof(hostApplicationLifetime));
        if (provider == null)
            throw new ArgumentNullException(nameof(provider));

        return new IWebSocketHandler<TSchema>[] {
            new WebSocketHandler<TSchema>(serializer, documentExecuter, serviceScopeFactory, options,
                hostApplicationLifetime, provider.GetService<IWebSocketAuthenticationService>()),
        };
    }

    ******************************/

    /// <inheritdoc/>
    protected override async Task<ExecutionResult> ExecuteScopedRequestAsync(HttpContext context, GraphQLRequest? request, IDictionary<string, object?> userContext)
    {
        var scope = _serviceScopeFactory.CreateScope();
        if (scope is IAsyncDisposable ad)
        {
            await using (ad.ConfigureAwait(false))
                return await ExecuteRequestAsync(context, request, scope.ServiceProvider, userContext);
        }
        else
        {
            using (scope)
                return await ExecuteRequestAsync(context, request, scope.ServiceProvider, userContext);
        }
    }

    /// <inheritdoc/>
    protected override async Task<ExecutionResult> ExecuteRequestAsync(HttpContext context, GraphQLRequest? request, IServiceProvider serviceProvider, IDictionary<string, object?> userContext)
    {
        var opts = new ExecutionOptions
        {
            Query = request?.Query,
            Variables = request?.Variables,
            Extensions = request?.Extensions,
            CancellationToken = context.RequestAborted,
            OperationName = request?.OperationName,
            RequestServices = serviceProvider,
            UserContext = userContext,
        };
        if (!context.WebSockets.IsWebSocketRequest)
        {
            if (HttpMethods.IsGet(context.Request.Method))
            {
                opts.ValidationRules = _getValidationRules;
                opts.CachedDocumentValidationRules = _getCachedDocumentValidationRules;
            }
            else if (HttpMethods.IsPost(context.Request.Method))
            {
                opts.ValidationRules = _postValidationRules;
                opts.CachedDocumentValidationRules = _postCachedDocumentValidationRules;
            }
        }
        return await _documentExecuter.ExecuteAsync(opts);
    }
}

/// <summary>
/// ASP.NET Core middleware for processing GraphQL requests. Handles both single and batch requests,
/// and dispatches WebSocket requests to the registered <see cref="IWebSocketHandler"/>.
/// </summary>
public abstract class GraphQLHttpMiddleware
{
    private readonly IGraphQLTextSerializer _serializer;
    //private readonly IEnumerable<IWebSocketHandler>? _webSocketHandlers;
    private readonly RequestDelegate _next;

    private const string QUERY_KEY = "query";
    private const string VARIABLES_KEY = "variables";
    private const string EXTENSIONS_KEY = "extensions";
    private const string OPERATION_NAME_KEY = "operationName";
    private const string MEDIATYPE_GRAPHQLJSON = "application/graphql+json";
    private const string MEDIATYPE_JSON = "application/json";
    private const string MEDIATYPE_GRAPHQL = "application/graphql";
    private const string CONTENTTYPE_GRAPHQLJSON = "application/graphql+json; charset=utf-8";

    /// <summary>
    /// Gets the options configured for this instance.
    /// </summary>
    protected GraphQLHttpMiddlewareOptions Options { get; }

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public GraphQLHttpMiddleware(
        RequestDelegate next,
        IGraphQLTextSerializer serializer,
        GraphQLHttpMiddlewareOptions options /*,
        IEnumerable<IWebSocketHandler>? webSocketHandlers = null */)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        //_webSocketHandlers = webSocketHandlers;
    }

    /// <inheritdoc/>
    public virtual async Task InvokeAsync(HttpContext context)
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            /************* WebSocket support ************
            if (Options.HandleWebSockets)
            {
                if (await HandleAuthorizeWebSocketConnectionAsync(context, _next))
                    return;
                // Process WebSocket request
                await HandleWebSocketAsync(context, _next);
            }
            else
            {
                await HandleInvalidHttpMethodErrorAsync(context, _next);
            }
            ************************/
            await HandleInvalidHttpMethodErrorAsync(context, _next);
            return;
        }

        // Handle requests as per recommendation at http://graphql.org/learn/serving-over-http/
        // Inspiration: https://github.com/graphql/express-graphql/blob/master/src/index.js
        var httpRequest = context.Request;
        var httpResponse = context.Response;

        // GraphQL HTTP only supports GET and POST methods
        bool isGet = HttpMethods.IsGet(httpRequest.Method);
        bool isPost = HttpMethods.IsPost(httpRequest.Method);
        if (isGet && !Options.HandleGet || isPost && !Options.HandlePost || !isGet && !isPost)
        {
            await HandleInvalidHttpMethodErrorAsync(context, _next);
            return;
        }

        // Authenticate request if necessary
        if (await HandleAuthorizeAsync(context, _next))
            return;

        // Parse POST body
        GraphQLRequest? bodyGQLRequest = null;
        IList<GraphQLRequest?>? bodyGQLBatchRequest = null;
        if (isPost)
        {
            if (!MediaTypeHeaderValue.TryParse(httpRequest.ContentType, out var mediaTypeHeader))
            {
                await HandleContentTypeCouldNotBeParsedErrorAsync(context, _next);
                return;
            }

            switch (mediaTypeHeader.MediaType?.ToLowerInvariant())
            {
                case MEDIATYPE_GRAPHQLJSON:
                case MEDIATYPE_JSON:
                    IList<GraphQLRequest?>? deserializationResult;
                    try
                    {
#if NET5_0_OR_GREATER
                        if (!TryGetEncoding(mediaTypeHeader.CharSet, out var sourceEncoding))
                        {
                            await HandleContentTypeCouldNotBeParsedErrorAsync(context, _next);
                            return;
                        }
                        // Wrap content stream into a transcoding stream that buffers the data transcoded from the sourceEncoding to utf-8.
                        if (sourceEncoding != null && sourceEncoding != System.Text.Encoding.UTF8)
                        {
                            using var tempStream = System.Text.Encoding.CreateTranscodingStream(httpRequest.Body, innerStreamEncoding: sourceEncoding, outerStreamEncoding: System.Text.Encoding.UTF8, leaveOpen: true);
                            deserializationResult = await _serializer.ReadAsync<IList<GraphQLRequest?>>(tempStream, context.RequestAborted);
                        }
                        else
                        {
                            deserializationResult = await _serializer.ReadAsync<IList<GraphQLRequest?>>(httpRequest.Body, context.RequestAborted);
                        }
#else
                        deserializationResult = await _serializer.ReadAsync<IList<GraphQLRequest?>>(httpRequest.Body, context.RequestAborted);
#endif
                    }
                    catch (Exception ex)
                    {
                        if (!await HandleDeserializationErrorAsync(context, _next, ex))
                            throw;
                        return;
                    }
                    // https://github.com/graphql-dotnet/server/issues/751
                    if (deserializationResult is GraphQLRequest[] array && array.Length == 1)
                        bodyGQLRequest = deserializationResult[0];
                    else
                        bodyGQLBatchRequest = deserializationResult;
                    break;

                case MEDIATYPE_GRAPHQL:
                    bodyGQLRequest = await DeserializeFromGraphBodyAsync(httpRequest.Body);
                    break;

                default:
                    if (httpRequest.HasFormContentType)
                    {
                        var formCollection = await httpRequest.ReadFormAsync(context.RequestAborted);
                        try
                        {
                            bodyGQLRequest = DeserializeFromFormBody(formCollection);
                        }
                        catch (Exception ex)
                        {
                            if (!await HandleDeserializationErrorAsync(context, _next, ex))
                                throw;
                            return;
                        }
                        break;
                    }
                    await HandleInvalidContentTypeErrorAsync(context, _next);
                    return;
            }
        }

        if (bodyGQLBatchRequest == null)
        {
            // If we don't have a batch request, parse the query from URL too to determine the actual request to run.
            // Query string params take priority.
            GraphQLRequest? gqlRequest = null;
            GraphQLRequest? urlGQLRequest = null;
            if (isGet || Options.ReadQueryStringOnPost)
            {
                try
                {
                    urlGQLRequest = DeserializeFromQueryString(httpRequest.Query);
                }
                catch (Exception ex)
                {
                    if (!await HandleDeserializationErrorAsync(context, _next, ex))
                        throw;
                    return;
                }
            }

            gqlRequest = new GraphQLRequest
            {
                Query = urlGQLRequest?.Query ?? bodyGQLRequest?.Query,
                Variables = urlGQLRequest?.Variables ?? bodyGQLRequest?.Variables,
                Extensions = urlGQLRequest?.Extensions ?? bodyGQLRequest?.Extensions,
                OperationName = urlGQLRequest?.OperationName ?? bodyGQLRequest?.OperationName
            };

            await HandleRequestAsync(context, _next, gqlRequest);
        }
        else if (Options.EnableBatchedRequests)
        {
            await HandleBatchRequestAsync(context, _next, bodyGQLBatchRequest);
        }
        else
        {
            await HandleBatchedRequestsNotSupportedAsync(context, _next);
        }
    }

    /// <summary>
    /// Perform authentication, if required, and return <see langword="true"/> if the
    /// request was handled (typically by returning an error message).  If <see langword="false"/>
    /// is returned, the request is processed normally.
    /// </summary>
    protected virtual async ValueTask<bool> HandleAuthorizeAsync(HttpContext context, RequestDelegate next)
    {
        var success = await AuthorizationHelper.AuthorizeAsync(
            new AuthorizationParameters<(GraphQLHttpMiddleware Middleware, HttpContext Context, RequestDelegate Next)>(
                context,
                Options,
                static info => info.Middleware.HandleNotAuthenticatedAsync(info.Context, info.Next),
                static info => info.Middleware.HandleNotAuthorizedRoleAsync(info.Context, info.Next),
                static (info, result) => info.Middleware.HandleNotAuthorizedPolicyAsync(info.Context, info.Next, result)),
            (this, context, next));

        return !success;
    }

    /// <summary>
    /// Perform authorization, if required, and return <see langword="true"/> if the
    /// request was handled (typically by returning an error message).  If <see langword="false"/>
    /// is returned, the request is processed normally.
    /// <br/><br/>
    /// By default this does not check authorization rules because authentication may take place within
    /// the WebSocket connection during the ConnectionInit message.  Authorization checks for
    /// WebSocket connections occur then, after authorization has taken place.
    /// </summary>
    protected virtual ValueTask<bool> HandleAuthorizeWebSocketConnectionAsync(HttpContext context, RequestDelegate next)
        => new(false);

    /// <summary>
    /// Handles a single GraphQL request.
    /// </summary>
    protected virtual async Task HandleRequestAsync(
        HttpContext context,
        RequestDelegate next,
        GraphQLRequest gqlRequest)
    {
        // Normal execution with single graphql request
        var userContext = await BuildUserContextAsync(context);
        var result = await ExecuteRequestAsync(context, gqlRequest, context.RequestServices, userContext);
        var statusCode = Options.ValidationErrorsReturnBadRequest && !result.Executed
            ? HttpStatusCode.BadRequest
            : HttpStatusCode.OK;
        await WriteJsonResponseAsync(context, statusCode, result);
    }

    /// <summary>
    /// Handles a batched GraphQL request.
    /// </summary>
    protected virtual async Task HandleBatchRequestAsync(
        HttpContext context,
        RequestDelegate next,
        IList<GraphQLRequest?> gqlRequests)
    {
        var userContext = await BuildUserContextAsync(context);
        var results = new ExecutionResult[gqlRequests.Count];
        if (gqlRequests.Count == 1)
        {
            results[0] = await ExecuteRequestAsync(context, gqlRequests[0], context.RequestServices, userContext);
        }
        else
        {
            // Batched execution with multiple graphql requests
            if (Options.ExecuteBatchedRequestsInParallel)
            {
                var resultTasks = new Task<ExecutionResult>[gqlRequests.Count];
                for (int i = 0; i < gqlRequests.Count; i++)
                {
                    resultTasks[i] = ExecuteScopedRequestAsync(context, gqlRequests[i], userContext);
                }
                await Task.WhenAll(resultTasks);
                for (int i = 0; i < gqlRequests.Count; i++)
                {
                    results[i] = await resultTasks[i];
                }
            }
            else
            {
                for (int i = 0; i < gqlRequests.Count; i++)
                {
                    results[i] = await ExecuteRequestAsync(context, gqlRequests[i], context.RequestServices, userContext);
                }
            }
        }
        await WriteJsonResponseAsync(context, HttpStatusCode.OK, results);
    }

    /// <summary>
    /// Executes a GraphQL request with a scoped service provider.
    /// <br/><br/>
    /// Typically this method should create a service scope and call
    /// <see cref="ExecuteRequestAsync(HttpContext, GraphQLRequest, IServiceProvider, IDictionary{string, object?})">ExecuteRequestAsync</see>,
    /// disposing of the scope when the asynchronous operation completes.
    /// </summary>
    protected abstract Task<ExecutionResult> ExecuteScopedRequestAsync(HttpContext context, GraphQLRequest? request, IDictionary<string, object?> userContext);

    /// <summary>
    /// Executes a GraphQL request.
    /// <br/><br/>
    /// It is suggested to use the <see cref="HttpGetValidationRule"/> and
    /// <see cref="HttpPostValidationRule"/> to ensure that only query operations
    /// are executed for GET requests, and query or mutation operations for
    /// POST requests.
    /// This should be set in both <see cref="ExecutionOptions.ValidationRules"/> and
    /// <see cref="ExecutionOptions.CachedDocumentValidationRules"/>, as shown below:
    /// <code>
    /// var rule = isGet ? new HttpGetValidationRule() : new HttpPostValidationRule();
    /// options.ValidationRules = DocumentValidator.CoreRules.Append(rule);
    /// options.CachedDocumentValidationRules = new[] { rule };
    /// </code>
    /// </summary>
    protected abstract Task<ExecutionResult> ExecuteRequestAsync(HttpContext context, GraphQLRequest? request, IServiceProvider serviceProvider, IDictionary<string, object?> userContext);

    /// <summary>
    /// Builds the user context based on a <see cref="HttpContext"/>.
    /// <br/><br/>
    /// Note that for batch or WebSocket requests, the user context is created once
    /// and re-used for each GraphQL request or data event that applies to the same
    /// <see cref="HttpContext"/>.
    /// <br/><br/>
    /// To tailor the user context individually for each request, call
    /// <see cref="GraphQLBuilderExtensions.ConfigureExecutionOptions(DI.IGraphQLBuilder, Action{ExecutionOptions})"/>
    /// to set or modify the user context, pulling the HTTP context from
    /// <see cref="IHttpContextAccessor"/> via <see cref="ExecutionOptions.RequestServices"/>
    /// if needed.
    /// <br/><br/>
    /// By default this method pulls the registered <see cref="IUserContextBuilder"/>,
    /// if any, within the service scope and executes it to build the user context.
    /// In this manner, both scoped and singleton <see cref="IUserContextBuilder"/>
    /// instances are supported, although singleton instances are recommended.
    /// </summary>
    protected virtual async ValueTask<IDictionary<string, object?>> BuildUserContextAsync(HttpContext context)
    {
        var userContextBuilder = context.RequestServices.GetService<IUserContextBuilder>();
        var userContext = userContextBuilder == null
            ? new Dictionary<string, object?>()
            : await userContextBuilder.BuildUserContextAsync(context);
        return userContext;
    }

    /// <summary>
    /// Selects a response content type string based on the <see cref="HttpContext"/>.
    /// Defaults to <see cref="CONTENTTYPE_GRAPHQLJSON"/>.  Override this value for compatibility
    /// with non-conforming GraphQL clients.
    /// <br/><br/>
    /// Note that by default, the response will be written as UTF-8 encoded JSON, regardless
    /// of the content-type value here.  For more complex behavior patterns, override
    /// <see cref="WriteJsonResponseAsync{TResult}(HttpContext, HttpStatusCode, TResult)"/>.
    /// </summary>
    protected virtual string SelectResponseContentType(HttpContext context)
        => CONTENTTYPE_GRAPHQLJSON;

    /// <summary>
    /// Writes the specified object (usually a GraphQL response represented as an instance of <see cref="ExecutionResult"/>) as JSON to the HTTP response stream.
    /// </summary>
    protected virtual Task WriteJsonResponseAsync<TResult>(HttpContext context, HttpStatusCode httpStatusCode, TResult result)
    {
        context.Response.ContentType = SelectResponseContentType(context);
        context.Response.StatusCode = (int)httpStatusCode;

        return _serializer.WriteAsync(context.Response.Body, result, context.RequestAborted);
    }

    /****** WebSocket support *********
     
    /// <summary>
    /// Handles a WebSocket connection request.
    /// </summary>
    protected virtual async Task HandleWebSocketAsync(HttpContext context, RequestDelegate next)
    {
        if (_webSocketHandlers == null || !_webSocketHandlers.Any())
        {
            await next(context);
            return;
        }

        string selectedProtocol;
        IWebSocketHandler selectedHandler;
        // select a sub-protocol, preferring the first sub-protocol requested by the client
        foreach (var protocol in context.WebSockets.WebSocketRequestedProtocols)
        {
            foreach (var handler in _webSocketHandlers)
            {
                if (handler.SupportedSubProtocols.Contains(protocol))
                {
                    selectedProtocol = protocol;
                    selectedHandler = handler;
                    goto MatchedHandler;
                }
            }
        }

        await HandleWebSocketSubProtocolNotSupportedAsync(context, next);
        return;

    MatchedHandler:
        var socket = await context.WebSockets.AcceptWebSocketAsync(selectedProtocol);

        if (socket.SubProtocol != selectedProtocol)
        {
            await socket.CloseAsync(
                WebSocketCloseStatus.ProtocolError,
                $"Invalid sub-protocol; expected '{selectedProtocol}'",
                context.RequestAborted);
            return;
        }

        // Prepare user context
        var userContext = await BuildUserContextAsync(context);
        // Connect, then wait until the websocket has disconnected (and all subscriptions ended)
        await selectedHandler.ExecuteAsync(context, socket, selectedProtocol, userContext);
    }

    *****************************/

    /// <summary>
    /// Writes a '401 Access denied.' message to the output when the user is not authenticated.
    /// </summary>
    protected virtual Task HandleNotAuthenticatedAsync(HttpContext context, RequestDelegate next)
        => WriteErrorResponseAsync(context, HttpStatusCode.Unauthorized, new AccessDeniedError("schema"));

    /// <summary>
    /// Writes a '401 Access denied.' message to the output when the user fails the role checks.
    /// </summary>
    protected virtual Task HandleNotAuthorizedRoleAsync(HttpContext context, RequestDelegate next)
        => WriteErrorResponseAsync(context, HttpStatusCode.Unauthorized, new AccessDeniedError("schema") { RolesRequired = Options.AuthorizedRoles });

    /// <summary>
    /// Writes a '401 Access denied.' message to the output when the user fails the policy check.
    /// </summary>
    protected virtual Task HandleNotAuthorizedPolicyAsync(HttpContext context, RequestDelegate next, AuthorizationResult authorizationResult)
        => WriteErrorResponseAsync(context, HttpStatusCode.Unauthorized, new AccessDeniedError("schema") { PolicyRequired = Options.AuthorizedPolicy, PolicyAuthorizationResult = authorizationResult });

    /// <summary>
    /// Writes a '400 JSON body text could not be parsed.' message to the output.
    /// Return <see langword="false"/> to rethrow the exception or <see langword="true"/>
    /// if it has been handled.  By default returns <see langword="true"/>.
    /// </summary>
    protected virtual async ValueTask<bool> HandleDeserializationErrorAsync(HttpContext context, RequestDelegate next, Exception exception)
    {
        await WriteErrorResponseAsync(context, HttpStatusCode.BadRequest, new JsonInvalidError(exception));
        return true;
    }

    /// <summary>
    /// Writes a '400 Batched requests are not supported.' message to the output.
    /// </summary>
    protected virtual Task HandleBatchedRequestsNotSupportedAsync(HttpContext context, RequestDelegate next)
        => WriteErrorResponseAsync(context, HttpStatusCode.BadRequest, new BatchedRequestsNotSupportedError());

    /// <summary>
    /// Writes a '400 Invalid requested WebSocket sub-protocol(s).' message to the output.
    /// </summary>
    protected virtual Task HandleWebSocketSubProtocolNotSupportedAsync(HttpContext context, RequestDelegate next)
        => WriteErrorResponseAsync(context, HttpStatusCode.BadRequest, new WebSocketSubProtocolNotSupportedError(context.WebSockets.WebSocketRequestedProtocols));

    /// <summary>
    /// Writes a '415 Invalid Content-Type header: could not be parsed.' message to the output.
    /// </summary>
    protected virtual Task HandleContentTypeCouldNotBeParsedErrorAsync(HttpContext context, RequestDelegate next)
        => WriteErrorResponseAsync(context, HttpStatusCode.UnsupportedMediaType, new InvalidContentTypeError($"value '{context.Request.ContentType}' could not be parsed."));

    /// <summary>
    /// Writes a '415 Invalid Content-Type header: non-supported media type.' message to the output.
    /// </summary>
    protected virtual Task HandleInvalidContentTypeErrorAsync(HttpContext context, RequestDelegate next)
        => WriteErrorResponseAsync(context, HttpStatusCode.UnsupportedMediaType, new InvalidContentTypeError($"non-supported media type '{context.Request.ContentType}'. Must be '{MEDIATYPE_JSON}', '{MEDIATYPE_GRAPHQL}' or a form body."));

    /// <summary>
    /// Indicates that an unsupported HTTP method was requested.
    /// Executes the next delegate in the chain by default.
    /// </summary>
    protected virtual Task HandleInvalidHttpMethodErrorAsync(HttpContext context, RequestDelegate next)
    {
        //context.Response.Headers["Allow"] = Options.HandleGet && Options.HandlePost ? "GET, POST" : Options.HandleGet ? "GET" : Options.HandlePost ? "POST" : "";
        //return WriteErrorResponseAsync(context, $"Invalid HTTP method.{(Options.HandleGet || Options.HandlePost ? $" Only {(Options.HandleGet && Options.HandlePost ? "GET and POST are" : Options.HandleGet ? "GET is" : "POST is")} supported." : "")}", HttpStatusCode.MethodNotAllowed);
        return next(context);
    }

    /// <summary>
    /// Writes the specified error message as a JSON-formatted GraphQL response, with the specified HTTP status code.
    /// </summary>
    protected virtual Task WriteErrorResponseAsync(HttpContext context, HttpStatusCode httpStatusCode, string errorMessage)
        => WriteErrorResponseAsync(context, httpStatusCode, new ExecutionError(errorMessage));

    /// <summary>
    /// Writes the specified error as a JSON-formatted GraphQL response, with the specified HTTP status code.
    /// </summary>
    protected virtual Task WriteErrorResponseAsync(HttpContext context, HttpStatusCode httpStatusCode, ExecutionError executionError)
    {
        var result = new ExecutionResult
        {
            Errors = new ExecutionErrors
            {
                executionError
            },
        };

        return WriteJsonResponseAsync(context, httpStatusCode, result);
    }

    private GraphQLRequest DeserializeFromQueryString(IQueryCollection queryCollection) => new()
    {
        Query = queryCollection.TryGetValue(QUERY_KEY, out var queryValues) ? queryValues[0] : null,
        Variables = Options.ReadVariablesFromQueryString && queryCollection.TryGetValue(VARIABLES_KEY, out var variablesValues) ? _serializer.Deserialize<Inputs>(variablesValues[0]) : null,
        Extensions = Options.ReadExtensionsFromQueryString && queryCollection.TryGetValue(EXTENSIONS_KEY, out var extensionsValues) ? _serializer.Deserialize<Inputs>(extensionsValues[0]) : null,
        OperationName = queryCollection.TryGetValue(OPERATION_NAME_KEY, out var operationNameValues) ? operationNameValues[0] : null,
    };

    private GraphQLRequest DeserializeFromFormBody(IFormCollection formCollection) => new()
    {
        Query = formCollection.TryGetValue(QUERY_KEY, out var queryValues) ? queryValues[0] : null,
        Variables = formCollection.TryGetValue(VARIABLES_KEY, out var variablesValues) ? _serializer.Deserialize<Inputs>(variablesValues[0]) : null,
        Extensions = formCollection.TryGetValue(EXTENSIONS_KEY, out var extensionsValues) ? _serializer.Deserialize<Inputs>(extensionsValues[0]) : null,
        OperationName = formCollection.TryGetValue(OPERATION_NAME_KEY, out var operationNameValues) ? operationNameValues[0] : null,
    };

    /// <summary>
    /// Reads body of content type: application/graphql
    /// </summary>
    private static async Task<GraphQLRequest> DeserializeFromGraphBodyAsync(Stream bodyStream)
    {
        // do not close underlying HTTP connection
        using var streamReader = new StreamReader(bodyStream, leaveOpen: true);

        // read query text
        string query = await streamReader.ReadToEndAsync();

        // return request; application/graphql MediaType supports only query text
        return new GraphQLRequest { Query = query };
    }

#if NET5_0_OR_GREATER
    private static bool TryGetEncoding(string? charset, out System.Text.Encoding? encoding)
    {
        encoding = null;

        if (string.IsNullOrEmpty(charset))
            return true;

        try
        {
            // Remove at most a single set of quotes.
            if (charset.Length > 2 && charset[0] == '\"' && charset[^1] == '\"')
            {
                encoding = System.Text.Encoding.GetEncoding(charset[1..^1]);
            }
            else
            {
                encoding = System.Text.Encoding.GetEncoding(charset);
            }
        }
        catch (ArgumentException)
        {
            return false;
        }

        return true;
    }
#endif
}
