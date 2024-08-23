#pragma warning disable CA1716 // Identifiers should not match keywords

using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Primitives;
using MediaTypeHeaderValueMs = Microsoft.Net.Http.Headers.MediaTypeHeaderValue;

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
    // important: when using convention-based ASP.NET Core middleware, the first constructor is always used

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public GraphQLHttpMiddleware(
        RequestDelegate next,
        IGraphQLTextSerializer serializer,
        IDocumentExecuter<TSchema> documentExecuter,
        IServiceScopeFactory serviceScopeFactory,
        GraphQLHttpMiddlewareOptions options,
        IHostApplicationLifetime hostApplicationLifetime)
        : base(next, serializer, documentExecuter, serviceScopeFactory, options, hostApplicationLifetime)
    {
    }

    /// <inheritdoc/>
    protected override ValueTask<IDictionary<string, object?>?> BuildUserContextAsync(HttpContext context, object? payload)
    {
        var userContextBuilder = context.RequestServices.GetService<IUserContextBuilder<TSchema>>();
        return userContextBuilder == null
            ? base.BuildUserContextAsync(context, payload)
            : userContextBuilder.BuildUserContextAsync(context, payload);
    }
}

/// <summary>
/// ASP.NET Core middleware for processing GraphQL requests. Handles both single and batch requests,
/// as well as WebSocket requests.
/// </summary>
public class GraphQLHttpMiddleware : IUserContextBuilder
{
    private readonly IDocumentExecuter _documentExecuter;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private static readonly IEnumerable<IValidationRule> _getValidationRules;
    private static readonly IEnumerable<IValidationRule> _getCachedDocumentValidationRules;
    private static readonly IEnumerable<IValidationRule> _postValidationRules;
    private static readonly IEnumerable<IValidationRule> _postCachedDocumentValidationRules;
    private readonly IGraphQLTextSerializer _serializer;
    private readonly RequestDelegate _next;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly GraphQLHttpMiddlewareOptions _options;

    private const string QUERY_KEY = "query";
    private const string VARIABLES_KEY = "variables";
    private const string EXTENSIONS_KEY = "extensions";
    private const string OPERATION_NAME_KEY = "operationName";
    private const string DOCUMENT_ID_KEY = "documentId";
    private const string OPERATIONS_KEY = "operations"; // used for multipart/form-data requests per https://github.com/jaydenseric/graphql-multipart-request-spec
    private const string MAP_KEY = "map"; // used for multipart/form-data requests per https://github.com/jaydenseric/graphql-multipart-request-spec
    private const string MEDIATYPE_GRAPHQLJSON = "application/graphql+json"; // deprecated
    private const string MEDIATYPE_JSON = "application/json";
    private const string MEDIATYPE_GRAPHQL = "application/graphql";
    private const string CONTENTTYPE_JSON = "application/json; charset=utf-8";
    private const string CONTENTTYPE_GRAPHQLJSON = "application/graphql+json; charset=utf-8"; // deprecated
    internal const string CONTENTTYPE_GRAPHQLRESPONSEJSON = "application/graphql-response+json; charset=utf-8";

    static GraphQLHttpMiddleware()
    {
        var getRule = new HttpGetValidationRule();
        _getValidationRules = DocumentValidator.CoreRules.Append(getRule).ToArray();
        _getCachedDocumentValidationRules = new[] { getRule };
        var postRule = new HttpPostValidationRule();
        _postValidationRules = DocumentValidator.CoreRules.Append(postRule).ToArray();
        _postCachedDocumentValidationRules = new[] { postRule };
    }

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public GraphQLHttpMiddleware(
        RequestDelegate next,
        IGraphQLTextSerializer serializer,
        IDocumentExecuter documentExecuter,
        IServiceScopeFactory serviceScopeFactory,
        GraphQLHttpMiddlewareOptions options,
        IHostApplicationLifetime hostApplicationLifetime)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _documentExecuter = documentExecuter ?? throw new ArgumentNullException(nameof(documentExecuter));
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _hostApplicationLifetime = hostApplicationLifetime ?? throw new ArgumentNullException(nameof(hostApplicationLifetime));
    }

    /// <inheritdoc cref="InvokeAsync(HttpContext, RequestDelegate)" />
    public virtual Task InvokeAsync(HttpContext context)
        => InvokeAsync(context, _next);

    /// <inheritdoc cref="IMiddleware.InvokeAsync(HttpContext, RequestDelegate)" />
    protected virtual async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            if (_options.HandleWebSockets)
            {
                if (await HandleAuthorizeWebSocketConnectionAsync(context, next))
                    return;

                await HandleWebSocketAsync(context, next);
            }
            else
            {
                await HandleInvalidHttpMethodErrorAsync(context, next);
            }
            return;
        }

        // Handle requests as per recommendation at http://graphql.org/learn/serving-over-http/
        // Inspiration: https://github.com/graphql/express-graphql/blob/master/src/index.js
        var httpRequest = context.Request;

        // GraphQL HTTP only supports GET and POST methods
        bool isGet = HttpMethods.IsGet(httpRequest.Method);
        bool isPost = HttpMethods.IsPost(httpRequest.Method);
        if (isGet && !_options.HandleGet || isPost && !_options.HandlePost || !isGet && !isPost)
        {
            await HandleInvalidHttpMethodErrorAsync(context, next);
            return;
        }

        // Perform CSRF protection if necessary
        if (await HandleCsrfProtectionAsync(context, next))
            return;

        // Authenticate request if necessary
        if (await HandleAuthorizeAsync(context, next))
            return;

        // Parse POST body
        GraphQLRequest? bodyGQLRequest = null;
        IList<GraphQLRequest?>? bodyGQLBatchRequest = null;
        if (isPost)
        {
            if (!MediaTypeHeaderValue.TryParse(httpRequest.ContentType, out var mediaTypeHeader))
            {
                await HandleContentTypeCouldNotBeParsedErrorAsync(context, next);
                return;
            }

            if (!TryGetEncoding(mediaTypeHeader.CharSet, out var sourceEncoding))
            {
                await HandleContentTypeCouldNotBeParsedErrorAsync(context, next);
                return;
            }

            var singleOrBatchRequest = await ReadPostContentAsync(context, next, mediaTypeHeader.MediaType, sourceEncoding);
            if (singleOrBatchRequest.HasValue)
                (bodyGQLRequest, bodyGQLBatchRequest) = singleOrBatchRequest.Value;
            else
                return;
        }

        if (bodyGQLBatchRequest == null)
        {
            // If we don't have a batch request, parse the query from URL too to determine the actual request to run.
            // Query string params take priority.
            GraphQLRequest? gqlRequest = null;
            GraphQLRequest? urlGQLRequest = null;
            if (isGet || _options.ReadQueryStringOnPost)
            {
                try
                {
                    urlGQLRequest = DeserializeFromQueryString(httpRequest.Query);
                }
                catch (Exception ex)
                {
                    if (!await HandleDeserializationErrorAsync(context, next, ex))
                        throw;
                    return;
                }
            }

            gqlRequest = new GraphQLRequest
            {
                Query = urlGQLRequest?.Query ?? bodyGQLRequest?.Query,
                Variables = urlGQLRequest?.Variables ?? bodyGQLRequest?.Variables,
                Extensions = urlGQLRequest?.Extensions ?? bodyGQLRequest?.Extensions,
                OperationName = urlGQLRequest?.OperationName ?? bodyGQLRequest?.OperationName,
                DocumentId = urlGQLRequest?.DocumentId ?? bodyGQLRequest?.DocumentId,
            };

            await HandleRequestAsync(context, next, gqlRequest);
        }
        else if (_options.EnableBatchedRequests)
        {
            await HandleBatchRequestAsync(context, next, bodyGQLBatchRequest);
        }
        else
        {
            await HandleBatchedRequestsNotSupportedAsync(context, next);
        }
    }

    /// <summary>
    /// Parses a request into a single or batch <see cref="GraphQLRequest"/> instance.
    /// <br/><br/>
    /// In case of an error, this method will handle the request (e.g. by calling
    /// <see cref="WriteErrorResponseAsync(HttpContext, HttpStatusCode, ExecutionError)">WriteErrorResponseAsync</see>)
    /// and return <see langword="null"/>.
    /// </summary>
    protected virtual async Task<(GraphQLRequest? SingleRequest, IList<GraphQLRequest?>? BatchRequest)?> ReadPostContentAsync(
        HttpContext context, RequestDelegate next, string? mediaType, Encoding? sourceEncoding)
    {
        var httpRequest = context.Request;

        switch (mediaType?.ToLowerInvariant())
        {
            case MEDIATYPE_GRAPHQLJSON: // deprecated
            case MEDIATYPE_JSON:
                IList<GraphQLRequest?>? deserializationResult;
                try
                {
#if NET5_0_OR_GREATER
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
                    return null;
                }
                // https://github.com/graphql-dotnet/server/issues/751
                if (deserializationResult is GraphQLRequest[] array && array.Length == 1)
                    return (deserializationResult[0], null);
                else
                    return (null, deserializationResult);

            case MEDIATYPE_GRAPHQL:
                return (await DeserializeFromGraphBodyAsync(httpRequest.Body, sourceEncoding), null);

            default:
                if (httpRequest.HasFormContentType && _options.ReadFormOnPost)
                {
                    try
                    {
                        var formCollection = await httpRequest.ReadFormAsync(context.RequestAborted);
                        return ReadFormContent(formCollection);
                    }
                    catch (ExecutionError ex) // catches FileCountExceededError, FileSizeExceededError, InvalidMapError
                    {
                        await WriteErrorResponseAsync(context, ex);
                        return null;
                    }
                    catch (Exception ex) // catches JSON deserialization exceptions
                    {
                        if (!await HandleDeserializationErrorAsync(context, _next, ex))
                            throw;
                        return null;
                    }
                }
                await HandleInvalidContentTypeErrorAsync(context, _next);
                return null;
        }
    }

    /// <summary>
    /// This method looks for an 'operations' key with a JSON value representing the GraphQL request(s)
    /// and a 'map' key with a JSON object value mapping file keys to variables in the request(s).
    /// See: <see href="https://github.com/jaydenseric/graphql-multipart-request-spec"/>.
    /// <para>
    /// If no 'operations' key exists, then falls back to looking for 'query', 'operationName', 'variables' and 'extensions' keys.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Note that 'operations' and 'map' keys are searched for even with application/x-www-form-urlencoded requests, but
    /// this should not be a problem.  Also, JSON deserialization may throw an exception by the JSON serialization engine in use.
    /// </remarks>
    /// <exception cref="FileCountExceededError"></exception>
    /// <exception cref="FileSizeExceededError"></exception>
    /// <exception cref="InvalidMapError"></exception>
    private (GraphQLRequest? SingleRequest, IList<GraphQLRequest?>? BatchRequest)? ReadFormContent(IFormCollection formCollection)
    {
        var operationsString = formCollection.TryGetValue(OPERATIONS_KEY, out var operationsValue) ? operationsValue[0] : null;
        var deserializationResult = _serializer.Deserialize<IList<GraphQLRequest?>>(operationsString)
            ?? new GraphQLRequest[] { DeserializeFromFormBody(formCollection) };

        var mapString = formCollection.TryGetValue(MAP_KEY, out var mapValue) ? mapValue[0] : null;
        var map = _serializer.Deserialize<Dictionary<string, string?[]>>(mapString);
        if (map != null)
            ApplyMapToRequests(map, formCollection, deserializationResult);

        // GraphQL serializers will deserialize a single request object as an array of a single request,
        // and an array of requests as a List<T> of requests, so we can identify which way it was encoded,
        // which is important for the response format.
        if (deserializationResult is GraphQLRequest[] array && array.Length == 1)
            return (deserializationResult[0], null);
        else
            return (null, deserializationResult);

        // Applies uploaded files onto request variables based on a provided map.
        // Validates file count and size.
        // Expected map format: { "abc": ["variables.file"] } where abc is the form field name of the uploaded file.
        // Also supports batch requests: { "abc": ["0.variables.file"] }
        // Also supports mapping one file to multiple variables: { "abc": ["variables.file1", "variables.file2"] }
        void ApplyMapToRequests(Dictionary<string, string?[]> map, IFormCollection form, IList<GraphQLRequest?> requests)
        {
            // validate file count
            if (_options.MaximumFileCount.HasValue && form.Files.Count > _options.MaximumFileCount.Value)
                throw new FileCountExceededError();

            // validate each file's size
            foreach (var file in form.Files)
            {
                if (_options.MaximumFileSize.HasValue && _options.MaximumFileSize.Value < file.Length)
                    throw new FileSizeExceededError();
            }

            foreach (var entry in map)
            {
                // validate entry key
                if (entry.Key == "" || entry.Key == QUERY_KEY || entry.Key == OPERATION_NAME_KEY || entry.Key == VARIABLES_KEY || entry.Key == EXTENSIONS_KEY || entry.Key == DOCUMENT_ID_KEY || entry.Key == OPERATIONS_KEY || entry.Key == MAP_KEY)
                    throw new InvalidMapError("Map key cannot be query, operationName, variables, extensions, documentId, operations or map.");
                // locate file
                var file = form.Files[entry.Key]
                    ?? throw new InvalidMapError("Map key does not refer to an uploaded file.");
                // apply file to each target
                foreach (var target in entry.Value)
                {
                    if (target == null)
                        throw new InvalidMapError("Map target cannot be null.");
                    ApplyFileToRequests(file, target, requests);
                }
            }
        }

        // Applies an uploaded file to a specific target property within a list of requests.
        // Expects a target string in the format of "variables.foo.bar" or "0.variables.foo.bar".
        static void ApplyFileToRequests(IFormFile file, string target, IList<GraphQLRequest?> requests)
        {
            if (target.StartsWith("variables.", StringComparison.Ordinal))
            {
                if (requests.Count < 1)
                    throw new InvalidMapError("No request specified.");
                ApplyFileToRequest(file, target.Substring(10), requests[0]);
                return;
            }
            var i = target.IndexOf('.');

#if NETCOREAPP3_1_OR_GREATER
            if (i == -1 || target.Length < 10 + i + 1 || !target.AsSpan(i + 1, 10).Equals("variables.", StringComparison.Ordinal))
#else
            if (i == -1 || target.Length < 10 + i + 1 || !string.Equals(target.Substring(i + 1, 10), "variables.", StringComparison.Ordinal))
#endif
                throw new InvalidMapError("Map path must start with 'variables.' or the index of the request followed by '.variables.'.");

#if NETCOREAPP3_1_OR_GREATER
            if (!int.TryParse(target.AsSpan(0, i), NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
#else
            if (!int.TryParse(target.Substring(0, i), NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
#endif
                throw new InvalidMapError("Could not parse the request index.");

            if (requests.Count < (index + 1))
                throw new InvalidMapError("Invalid request index.");

            ApplyFileToRequest(file, target.Substring(10 + i + 1), requests[index]);
        }

        // Applies an uploaded file to a specific target property within a GraphQLRequest.
        // Expects a target string in the format of "foo.bar".
        static void ApplyFileToRequest(IFormFile file, string target, GraphQLRequest? request)
        {
            // Ensure request's Variables are not null, else throw an error.
            var variables = request?.Variables ?? throw new InvalidMapError("No variables defined for this request.");

            // Define the parent object and pointer to index or child key within 
            object parent = variables;
            string? prop = null;
            // Iterate over each segment of the target path
            foreach (var location in target.Split('.'))
            {
                if (location == "")
                    throw new InvalidMapError("Empty property name.");
                // If this is the first segment, it is the property name.
                if (prop == null)
                {
                    prop = location;
                    continue;
                }

                // First, resolve the prior segment to an object

                // Handle lists
                if (parent is IList list)
                {
                    // Parse the index, ensure it is within bounds, and get the child object.
                    if (!int.TryParse(prop, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
                        throw new InvalidMapError($"Child index '{prop}' is not an integer.");
                    if (list.Count < (index + 1) || index < 0)
                        throw new InvalidMapError($"Index '{index}' is out of bounds.");
                    parent = list[index] ?? throw new InvalidMapError($"Child index '{index}' refers to a null object.");
                }
                // Handle objects
                else if (parent is IReadOnlyDictionary<string, object?> dic)
                {
                    // Ensure the child property exists and get the child object.
                    if (!dic.TryGetValue(prop, out var value))
                        throw new InvalidMapError($"Child property '{prop}' does not exist.");
                    parent = value ??
                        throw new InvalidMapError($"Child property '{prop}' refers to a null object.");
                }
                else
                {
                    throw new InvalidMapError($"Cannot refer to child property '{prop}' of a string or number.");
                }

                // Then, set the child property key or index
                prop = location;
            }

            // Verify that the target is valid (should not be possible)
            Debug.Assert(prop != null);
            Debug.Assert(prop!.Length > 0);

            // Resolve the segment, and set it to the form file

            // Handle lists
            if (parent is IList list2)
            {
                // Parse the index, ensure it is within bounds, and set the child object.
                if (!int.TryParse(prop, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
                    throw new InvalidMapError($"Child index '{prop}' is not an integer.");
                if (list2.Count < (index + 1) || index < 0)
                    throw new InvalidMapError($"Index '{index}' is out of bounds.");
                if (list2[index] != null)
                    throw new InvalidMapError($"Index '{index}' must refer to a null variable.");
                list2[index] = file;
            }
            // Handle objects
            else if (parent is IDictionary<string, object?> dic)
            {
                // Ensure the child property exists and set the child object.
                if (!dic.TryGetValue(prop, out var value))
                    throw new InvalidMapError($"Child property '{prop}' does not exist.");
                else if (value != null)
                    throw new InvalidMapError($"Child property '{prop}' must refer to a null object.");
                if (parent == variables)
                {
                    // unfortunate design due to Inputs being readonly
                    request.Variables = new Inputs(new Dictionary<string, object?>(variables)
                    {
                        [prop] = file
                    });
                }
                else
                    dic[prop] = file;
            }
            else
            {
                throw new InvalidMapError($"Cannot refer to child property '{prop}' of a string or number.");
            }
        }
    }

    /// <summary>
    /// Performs CSRF protection, if required, and returns <see langword="true"/> if the
    /// request was handled (typically by returning an error message).  If <see langword="false"/>
    /// is returned, the request is processed normally.
    /// </summary>
    protected virtual async ValueTask<bool> HandleCsrfProtectionAsync(HttpContext context, RequestDelegate next)
    {
        if (!_options.CsrfProtectionEnabled)
            return false;
        if (context.Request.Headers.TryGetValue("Content-Type", out var contentTypes) && contentTypes.Count > 0 && contentTypes[0] != null)
        {
            var contentType = contentTypes[0]!;
            if (contentType.IndexOf(';') > 0)
            {
                contentType = contentType.Substring(0, contentType.IndexOf(';'));
            }
            contentType = contentType.Trim().ToLowerInvariant();
            if (!(contentType == "text/plain" || contentType == "application/x-www-form-urlencoded" || contentType == "multipart/form-data"))
                return false;
        }
        foreach (var header in _options.CsrfProtectionHeaders)
        {
            if (context.Request.Headers.TryGetValue(header, out var values) && values.Count > 0 && values[0]?.Length > 0)
                return false;
        }
        await HandleCsrfProtectionErrorAsync(context, next);
        return true;
    }

    /// <summary>
    /// Perform authentication, if required, and returns <see langword="true"/> if the
    /// request was handled (typically by returning an error message).  If <see langword="false"/>
    /// is returned, the request is processed normally.
    /// </summary>
    protected virtual async ValueTask<bool> HandleAuthorizeAsync(HttpContext context, RequestDelegate next)
    {
        await SetHttpContextUserAsync(context);

        var success = await AuthorizationHelper.AuthorizeAsync(
            new AuthorizationParameters<(GraphQLHttpMiddleware Middleware, HttpContext Context, RequestDelegate Next)>(
                context,
                _options,
                static info => info.Middleware.HandleNotAuthenticatedAsync(info.Context, info.Next),
                static info => info.Middleware.HandleNotAuthorizedRoleAsync(info.Context, info.Next),
                static (info, result) => info.Middleware.HandleNotAuthorizedPolicyAsync(info.Context, info.Next, result)),
            (this, context, next));

        return !success;
    }

    /// <summary>
    /// If any authentication schemes are defined, set the <see cref="HttpContext.User"/> property.
    /// </summary>
    private async ValueTask SetHttpContextUserAsync(HttpContext context)
    {
        if (_options.AuthenticationSchemes.Count > 0)
        {
            ClaimsPrincipal? newPrincipal = null;
            foreach (var scheme in _options.AuthenticationSchemes)
            {
                var result = await context.AuthenticateAsync(scheme);
                if (result != null && result.Succeeded)
                {
                    newPrincipal = SecurityHelper.MergeUserPrincipal(newPrincipal, result.Principal);
                }
            }
            context.User = newPrincipal ?? new ClaimsPrincipal(new ClaimsIdentity());
        }
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
    protected virtual async ValueTask<bool> HandleAuthorizeWebSocketConnectionAsync(HttpContext context, RequestDelegate next)
    {
        await SetHttpContextUserAsync(context);
        return false;
    }

    /// <summary>
    /// Handles a single GraphQL request.
    /// </summary>
    protected virtual async Task HandleRequestAsync(
        HttpContext context,
        RequestDelegate next,
        GraphQLRequest gqlRequest)
    {
        // Normal execution with single graphql request
        var userContext = await BuildUserContextAsync(context, null);
        var result = await ExecuteRequestAsync(context, gqlRequest, context.RequestServices, userContext);
        // when the request fails validation (this logic does not apply to execution errors)
        if (!result.Executed)
        {
            // always return 405 Method Not Allowed when applicable, as this is a transport problem, not really a validation error,
            // even though it occurs during validation (because the query text must be parsed to know if the request is a query or a mutation)
            if (result.Errors?.Any(e => e is HttpMethodValidationError) == true)
            {
                await WriteJsonResponseAsync(context, HttpStatusCode.MethodNotAllowed, result);
                return;
            }
        }
        await WriteJsonResponseAsync(context, result);
    }

    /// <summary>
    /// Handles a batched GraphQL request.
    /// </summary>
    protected virtual async Task HandleBatchRequestAsync(
        HttpContext context,
        RequestDelegate next,
        IList<GraphQLRequest?> gqlRequests)
    {
        var userContext = await BuildUserContextAsync(context, null);
        var results = new ExecutionResult[gqlRequests.Count];
        if (gqlRequests.Count == 1)
        {
            results[0] = await ExecuteRequestAsync(context, gqlRequests[0], context.RequestServices, userContext);
        }
        else
        {
            // Batched execution with multiple graphql requests
            if (_options.ExecuteBatchedRequestsInParallel)
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
    protected virtual async Task<ExecutionResult> ExecuteScopedRequestAsync(HttpContext context, GraphQLRequest? request, IDictionary<string, object?>? userContext)
    {
        var scope = _serviceScopeFactory.CreateScope();
        try
        {
            return await ExecuteRequestAsync(context, request, scope.ServiceProvider, userContext);
        }
        finally
        {
            if (scope is IAsyncDisposable ad)
                await ad.DisposeAsync().ConfigureAwait(false);
            else
                scope.Dispose();
        }
    }

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
    protected virtual async Task<ExecutionResult> ExecuteRequestAsync(HttpContext context, GraphQLRequest? request, IServiceProvider serviceProvider, IDictionary<string, object?>? userContext)
    {
        var opts = new ExecutionOptions
        {
            Query = request?.Query,
            Variables = request?.Variables,
            Extensions = request?.Extensions,
            DocumentId = request?.DocumentId,
            CancellationToken = context.RequestAborted,
            OperationName = request?.OperationName,
            RequestServices = serviceProvider,
            User = context.User,
        };

        if (userContext != null)
            opts.UserContext = userContext;

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
    /// By default this method pulls the registered <see cref="IUserContextBuilder{TSchema}"/>
    /// or <see cref="IUserContextBuilder"/> instance, if any, within the service scope
    /// and executes it to build the user context.
    /// In this manner, both scoped and singleton <see cref="IUserContextBuilder"/>
    /// instances are supported, although singleton instances are recommended.
    /// </summary>
    protected virtual ValueTask<IDictionary<string, object?>?> BuildUserContextAsync(HttpContext context, object? payload)
    {
        var userContextBuilder = context.RequestServices.GetService<IUserContextBuilder>();
        return userContextBuilder == null
            ? default // successful result of null
            : userContextBuilder.BuildUserContextAsync(context, payload);
    }

    ValueTask<IDictionary<string, object?>?> IUserContextBuilder.BuildUserContextAsync(HttpContext context, object? payload)
        => BuildUserContextAsync(context, payload);

    private static readonly MediaTypeHeaderValueMs _applicationJsonMediaType = MediaTypeHeaderValueMs.Parse(CONTENTTYPE_JSON);
    private static readonly MediaTypeHeaderValueMs[] _validMediaTypes = new[]
    {
        MediaTypeHeaderValueMs.Parse(CONTENTTYPE_GRAPHQLRESPONSEJSON),
        _applicationJsonMediaType,
        MediaTypeHeaderValueMs.Parse(CONTENTTYPE_GRAPHQLJSON), // deprecated
    };

    /// <summary>
    /// Selects a response content type string based on the <see cref="HttpContext"/>.
    /// The default implementation attempts to match the content-type requested by the
    /// client through the 'Accept' HTTP header to the default content type specified
    /// within <see cref="GraphQLHttpMiddlewareOptions.DefaultResponseContentType"/>.
    /// If matched, the specified content-type is returned; if not, supported
    /// content-types are tested ("application/json", "application/graphql+json", and
    /// "application/graphql-response+json") to see if they match the 'Accept' header.
    /// <br/><br/>
    /// Note that by default, the response will be written as UTF-8 encoded JSON, regardless
    /// of the content-type value here, and this method's default implementation assumes as much.
    /// For more complex behavior patterns, override
    /// <see cref="WriteJsonResponseAsync{TResult}(HttpContext, HttpStatusCode, TResult)"/>.
    /// </summary>
    protected virtual MediaTypeHeaderValueMs SelectResponseContentType(HttpContext context)
    {
        // pull the Accept header, which may contain multiple content types
        var acceptHeaders = context.Request.Headers.ContainsKey(Microsoft.Net.Http.Headers.HeaderNames.Accept)
            ? context.Request.GetTypedHeaders().Accept
            : Array.Empty<MediaTypeHeaderValueMs>();

        if (acceptHeaders.Count == 1)
        {
            var response = IsSupportedMediaType(acceptHeaders[0]);
            if (response != null)
                return response;
        }
        else if (acceptHeaders.Count > 0)
        {
            // enumerate through each content type and see if it matches a supported content type
            // give priority to specific types, then to types with wildcards
            foreach (var acceptHeader in acceptHeaders.OrderBy(x => x.MatchesAllTypes ? 4 : x.MatchesAllSubTypes ? 3 : x.MatchesAllSubTypesWithoutSuffix ? 2 : 1))
            {
                var response = IsSupportedMediaType(acceptHeader);
                if (response != null)
                    return response;
            }
        }

        // return the default content type if no match is found, or if there is no 'Accept' header
        return _options.DefaultResponseContentType;
    }

    /// <summary>
    /// Checks to see if the specified <see cref="MediaTypeHeaderValueMs"/> matches any of the supported content types
    /// by this middleware.  If a match is found, the matching content type is returned; otherwise, <see langword="null"/>.
    /// Prioritizes <see cref="GraphQLHttpMiddlewareOptions.DefaultResponseContentType"/>, then
    /// <c>application/graphql-response+json</c>, then <c>application/json</c>.
    /// </summary>
    private MediaTypeHeaderValueMs? IsSupportedMediaType(MediaTypeHeaderValueMs acceptHeader)
        => IsSupportedMediaType(acceptHeader, _options.DefaultResponseContentType, _validMediaTypes);

    /// <summary>
    /// Checks to see if the specified <see cref="MediaTypeHeaderValueMs"/> matches any of the supported content types
    /// by this middleware.  If a match is found, the matching content type is returned; otherwise, <see langword="null"/>.
    /// Prioritizes <see cref="GraphQLHttpMiddlewareOptions.DefaultResponseContentType"/>, then
    /// <c>application/graphql-response+json</c>, then <c>application/json</c>.
    /// </summary>
    private static MediaTypeHeaderValueMs? IsSupportedMediaType(MediaTypeHeaderValueMs acceptHeader, MediaTypeHeaderValueMs preferredContentType, MediaTypeHeaderValueMs[] allowedContentTypes)
    {
        // speeds check in WriteJsonResponseAsync
        if (acceptHeader == preferredContentType)
            return preferredContentType;

        // strip quotes from charset
        if (acceptHeader.Charset.Length > 0 && acceptHeader.Charset[0] == '\"' && acceptHeader.Charset[acceptHeader.Charset.Length - 1] == '\"')
        {
            acceptHeader.Charset = acceptHeader.Charset.Substring(1, acceptHeader.Charset.Length - 2);
        }

        // check if this matches the default content type header
        if (IsSubsetOf(preferredContentType, acceptHeader))
            return preferredContentType;

        // if the default content type header does not contain a charset, test with utf-8 as the charset
        if (preferredContentType.Charset.Length == 0)
        {
            var contentType2 = preferredContentType.Copy();
            contentType2.Charset = "utf-8";
            if (IsSubsetOf(contentType2, acceptHeader))
                return contentType2;
        }

        // loop through the other supported media types, attempting to find a match
        for (int j = 0; j < allowedContentTypes.Length; j++)
        {
            var mediaType = allowedContentTypes[j];
            if (IsSubsetOf(mediaType, acceptHeader))
                // when a match is found, return the match
                return mediaType;
        }

        // no match
        return null;

        // --- note: the below functions were copied from ASP.NET Core 2.1 source ---
        // see https://github.com/dotnet/aspnetcore/blob/v2.1.33/src/Http/Headers/src/MediaTypeHeaderValue.cs

        // The ASP.NET Core 6.0 source contains logic that is not suitable -- it will consider
        // "application/graphql-response+json" to match an 'Accept' header of "application/json",
        // which can break client applications.

        /*
         * Copyright (c) .NET Foundation. All rights reserved.
         *
         * Licensed under the Apache License, Version 2.0 (the "License"); you may not use
         * these files except in compliance with the License. You may obtain a copy of the
         * License at
         *
         * http://www.apache.org/licenses/LICENSE-2.0
         *
         * Unless required by applicable law or agreed to in writing, software distributed
         * under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR
         * CONDITIONS OF ANY KIND, either express or implied. See the License for the
         * specific language governing permissions and limitations under the License.
         *
         */

        static bool IsSubsetOf(MediaTypeHeaderValueMs mediaType, MediaTypeHeaderValueMs otherMediaType)
        {
            // "text/plain" is a subset of "text/plain", "text/*" and "*/*". "*/*" is a subset only of "*/*".
            return MatchesType(mediaType, otherMediaType) &&
                MatchesSubtype(mediaType, otherMediaType) &&
                MatchesParameters(mediaType, otherMediaType);
        }

        static bool MatchesType(MediaTypeHeaderValueMs mediaType, MediaTypeHeaderValueMs set)
        {
            return set.MatchesAllTypes ||
                set.Type.Equals(mediaType.Type, StringComparison.OrdinalIgnoreCase);
        }

        static bool MatchesSubtype(MediaTypeHeaderValueMs mediaType, MediaTypeHeaderValueMs set)
        {
            if (set.MatchesAllSubTypes)
            {
                return true;
            }
            if (set.Suffix.HasValue)
            {
                if (mediaType.Suffix.HasValue)
                {
                    return MatchesSubtypeWithoutSuffix(mediaType, set) && MatchesSubtypeSuffix(mediaType, set);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return set.SubType.Equals(mediaType.SubType, StringComparison.OrdinalIgnoreCase);
            }
        }

        static bool MatchesSubtypeWithoutSuffix(MediaTypeHeaderValueMs mediaType, MediaTypeHeaderValueMs set)
        {
            return set.MatchesAllSubTypesWithoutSuffix ||
                set.SubTypeWithoutSuffix.Equals(mediaType.SubTypeWithoutSuffix, StringComparison.OrdinalIgnoreCase);
        }

        static bool MatchesParameters(MediaTypeHeaderValueMs mediaType, MediaTypeHeaderValueMs set)
        {
            if (set.Parameters.Count != 0)
            {
                // Make sure all parameters in the potential superset are included locally. Fine to have additional
                // parameters locally; they make this one more specific.
                foreach (var parameter in set.Parameters)
                {
                    if (parameter.Name.Equals("*", StringComparison.OrdinalIgnoreCase))
                    {
                        // A parameter named "*" has no effect on media type matching, as it is only used as an indication
                        // that the entire media type string should be treated as a wildcard.
                        continue;
                    }

                    if (parameter.Name.Equals("q", StringComparison.OrdinalIgnoreCase))
                    {
                        // "q" and later parameters are not involved in media type matching. Quoting the RFC: The first
                        // "q" parameter (if any) separates the media-range parameter(s) from the accept-params.
                        break;
                    }

                    var localParameter = Microsoft.Net.Http.Headers.NameValueHeaderValue.Find(mediaType.Parameters, parameter.Name);
                    if (localParameter == null)
                    {
                        // Not found.
                        return false;
                    }

                    if (!StringSegment.Equals(parameter.Value, localParameter.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        static bool MatchesSubtypeSuffix(MediaTypeHeaderValueMs mediaType, MediaTypeHeaderValueMs set)
            // We don't have support for wildcards on suffixes alone (e.g., "application/entity+*")
            // because there's no clear use case for it.
            => set.Suffix.Equals(mediaType.Suffix, StringComparison.OrdinalIgnoreCase);

        // --- end of ASP.NET Core 2.1 copied functions ---
    }

    /// <summary>
    /// Writes the specified <see cref="ExecutionResult"/> as JSON to the HTTP response stream,
    /// selecting the proper content type and status code based on the request Accept header and response.
    /// </summary>
    protected virtual Task WriteJsonResponseAsync(HttpContext context, ExecutionResult result)
    {
        var contentType = SelectResponseContentType(context);
        context.Response.ContentType = contentType == _options.DefaultResponseContentType ? _options.DefaultResponseContentTypeString : contentType.ToString();
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        if (result.Executed == false)
        {
            var useBadRequest = _options.ValidationErrorsReturnBadRequest ?? IsSupportedMediaType(contentType, _applicationJsonMediaType, Array.Empty<MediaTypeHeaderValueMs>()) == null;
            if (useBadRequest)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

                // if all errors being returned prefer the same status code, use that
                if (result.Errors?.Count > 0 && result.Errors[0] is IHasPreferredStatusCode initialError)
                {
                    if (result.Errors.All(e => e is IHasPreferredStatusCode e2 && e2.PreferredStatusCode == initialError.PreferredStatusCode))
                        context.Response.StatusCode = (int)initialError.PreferredStatusCode;
                }
            }
        }

        return _serializer.WriteAsync(context.Response.Body, result, context.RequestAborted);
    }

    /// <summary>
    /// Writes the specified object (usually a GraphQL response represented as an instance of <see cref="ExecutionResult"/>)
    /// as JSON to the HTTP response stream, using the specified status code.
    /// </summary>
    protected virtual Task WriteJsonResponseAsync<TResult>(HttpContext context, HttpStatusCode httpStatusCode, TResult result)
    {
        var contentType = SelectResponseContentType(context);
        context.Response.ContentType = contentType == _options.DefaultResponseContentType ? _options.DefaultResponseContentTypeString : contentType.ToString();
        context.Response.StatusCode = (int)httpStatusCode;

        return _serializer.WriteAsync(context.Response.Body, result, context.RequestAborted);
    }

    private static readonly IEnumerable<string> _supportedSubProtocols = new List<string>(new[]
    {
        WebSockets.GraphQLWs.SubscriptionServer.SubProtocol,
        WebSockets.SubscriptionsTransportWs.SubscriptionServer.SubProtocol,
    }).AsReadOnly();

    /// <summary>
    /// Gets a list of WebSocket sub-protocols supported.
    /// </summary>
    protected virtual IEnumerable<string> SupportedWebSocketSubProtocols => _supportedSubProtocols;

    /// <summary>
    /// Creates an <see cref="IWebSocketConnection"/>, a WebSocket message pump.
    /// </summary>
    protected virtual IWebSocketConnection CreateWebSocketConnection(HttpContext httpContext, WebSocket webSocket, CancellationToken cancellationToken)
        => new WebSocketConnection(httpContext, webSocket, _serializer, _options.WebSockets, cancellationToken);

    /// <summary>
    /// Builds an <see cref="IOperationMessageProcessor"/> for the specified sub-protocol.
    /// </summary>
    protected virtual IOperationMessageProcessor CreateMessageProcessor(IWebSocketConnection webSocketConnection, string subProtocol)
    {
        var authService = webSocketConnection.HttpContext.RequestServices.GetService<IWebSocketAuthenticationService>();

        if (subProtocol == WebSockets.GraphQLWs.SubscriptionServer.SubProtocol)
        {
            return new WebSockets.GraphQLWs.SubscriptionServer(
                webSocketConnection,
                _options.WebSockets,
                _options,
                _documentExecuter,
                _serializer,
                _serviceScopeFactory,
                this,
                authService);
        }
        else if (subProtocol == WebSockets.SubscriptionsTransportWs.SubscriptionServer.SubProtocol)
        {
            return new WebSockets.SubscriptionsTransportWs.SubscriptionServer(
                webSocketConnection,
                _options.WebSockets,
                _options,
                _documentExecuter,
                _serializer,
                _serviceScopeFactory,
                this,
                authService);
        }

        throw new ArgumentOutOfRangeException(nameof(subProtocol));
    }

    /// <summary>
    /// Handles a WebSocket connection request.
    /// </summary>
    protected virtual async Task HandleWebSocketAsync(HttpContext context, RequestDelegate next)
    {
        string? subProtocol = null;
        // select a sub-protocol, preferring the first sub-protocol requested by the client
        foreach (var protocol in context.WebSockets.WebSocketRequestedProtocols)
        {
            if (SupportedWebSocketSubProtocols.Contains(protocol))
            {
                subProtocol = protocol;
                break;
            }
        }

        if (subProtocol == null)
        {
            await HandleWebSocketSubProtocolNotSupportedAsync(context, next);
            return;
        }

        var webSocket = await context.WebSockets.AcceptWebSocketAsync(subProtocol);

        if (webSocket.SubProtocol != subProtocol)
        {
            await webSocket.CloseAsync(
                WebSocketCloseStatus.ProtocolError,
                $"Invalid sub-protocol; expected '{subProtocol}'",
                context.RequestAborted);
            return;
        }

        // Connect, then wait until the websocket has disconnected (and all subscriptions ended)
        var appStoppingToken = _hostApplicationLifetime.ApplicationStopping;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted, appStoppingToken);
        if (cts.Token.IsCancellationRequested)
            return;
        try
        {
            using var webSocketConnection = CreateWebSocketConnection(context, webSocket, cts.Token);
            using var messageProcessor = CreateMessageProcessor(webSocketConnection, subProtocol);
            await webSocketConnection.ExecuteAsync(messageProcessor);
        }
        catch (OperationCanceledException) when (appStoppingToken.IsCancellationRequested)
        {
            // terminate all pending WebSockets connections when the application is in the process of stopping

            // note: we are consuming OCE in this case because ASP.NET Core does not consider the task as canceled when
            // an OCE occurs that is not due to httpContext.RequestAborted; so to prevent ASP.NET Core from considering
            // this a "regular" exception, we consume it here
        }
    }

    /// <summary>
    /// Writes an access denied message to the output with status code <c>401 Unauthorized</c> when the user is not authenticated.
    /// </summary>
    protected virtual Task HandleNotAuthenticatedAsync(HttpContext context, RequestDelegate next)
        => WriteErrorResponseAsync(context, new AccessDeniedError("schema") { PreferredStatusCode = HttpStatusCode.Unauthorized });

    /// <summary>
    /// Writes an access denied message to the output with status code <c>403 Forbidden</c> when the user fails the role checks.
    /// </summary>
    protected virtual Task HandleNotAuthorizedRoleAsync(HttpContext context, RequestDelegate next)
        => WriteErrorResponseAsync(context, new AccessDeniedError("schema") { RolesRequired = _options.AuthorizedRoles });

    /// <summary>
    /// Writes an access denied message to the output with status code <c>403 Forbidden</c> when the user fails the policy check.
    /// </summary>
    protected virtual Task HandleNotAuthorizedPolicyAsync(HttpContext context, RequestDelegate next, AuthorizationResult authorizationResult)
        => WriteErrorResponseAsync(context, new AccessDeniedError("schema") { PolicyRequired = _options.AuthorizedPolicy, PolicyAuthorizationResult = authorizationResult });

    /// <summary>
    /// Writes a '400 JSON body text could not be parsed.' message to the output.
    /// Return <see langword="false"/> to rethrow the exception or <see langword="true"/>
    /// if it has been handled.  By default returns <see langword="true"/>.
    /// </summary>
    protected virtual async ValueTask<bool> HandleDeserializationErrorAsync(HttpContext context, RequestDelegate next, Exception exception)
    {
        await WriteErrorResponseAsync(context, new JsonInvalidError(exception));
        return true;
    }

    /// <summary>
    /// Writes a '.' message to the output.
    /// </summary>
    protected virtual async Task HandleCsrfProtectionErrorAsync(HttpContext context, RequestDelegate next)
    {
        await WriteErrorResponseAsync(context, new CsrfProtectionError(_options.CsrfProtectionHeaders));
    }

    /// <summary>
    /// Writes a '400 Batched requests are not supported.' message to the output.
    /// </summary>
    protected virtual Task HandleBatchedRequestsNotSupportedAsync(HttpContext context, RequestDelegate next)
        => WriteErrorResponseAsync(context, new BatchedRequestsNotSupportedError());

    /// <summary>
    /// Writes a '400 Invalid requested WebSocket sub-protocol(s).' message to the output.
    /// </summary>
    protected virtual Task HandleWebSocketSubProtocolNotSupportedAsync(HttpContext context, RequestDelegate next)
        => WriteErrorResponseAsync(context, new WebSocketSubProtocolNotSupportedError(context.WebSockets.WebSocketRequestedProtocols));

    /// <summary>
    /// Writes a '415 Invalid Content-Type header: could not be parsed.' message to the output.
    /// </summary>
    protected virtual Task HandleContentTypeCouldNotBeParsedErrorAsync(HttpContext context, RequestDelegate next)
        => WriteErrorResponseAsync(context, new InvalidContentTypeError($"value '{context.Request.ContentType}' could not be parsed."));

    /// <summary>
    /// Writes a '415 Invalid Content-Type header: non-supported media type.' message to the output.
    /// </summary>
    protected virtual Task HandleInvalidContentTypeErrorAsync(HttpContext context, RequestDelegate next)
        => WriteErrorResponseAsync(
            context,
            _options.ReadFormOnPost
            ? new InvalidContentTypeError($"non-supported media type '{context.Request.ContentType}'. Must be '{MEDIATYPE_JSON}', '{MEDIATYPE_GRAPHQL}' or a form body.")
            : new InvalidContentTypeError($"non-supported media type '{context.Request.ContentType}'. Must be '{MEDIATYPE_JSON}' or '{MEDIATYPE_GRAPHQL}'.")
        );

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
    /// Writes the specified error as a JSON-formatted GraphQL response.
    /// </summary>
    protected virtual Task WriteErrorResponseAsync(HttpContext context, ExecutionError executionError)
        => WriteErrorResponseAsync(context, executionError is IHasPreferredStatusCode withCode ? withCode.PreferredStatusCode : HttpStatusCode.BadRequest, executionError);

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
        Variables = _options.ReadVariablesFromQueryString && queryCollection.TryGetValue(VARIABLES_KEY, out var variablesValues) ? _serializer.Deserialize<Inputs>(variablesValues[0]) : null,
        Extensions = _options.ReadExtensionsFromQueryString && queryCollection.TryGetValue(EXTENSIONS_KEY, out var extensionsValues) ? _serializer.Deserialize<Inputs>(extensionsValues[0]) : null,
        OperationName = queryCollection.TryGetValue(OPERATION_NAME_KEY, out var operationNameValues) ? operationNameValues[0] : null,
        DocumentId = queryCollection.TryGetValue(DOCUMENT_ID_KEY, out var documentIdValues) ? documentIdValues[0] : null,
    };

    private GraphQLRequest DeserializeFromFormBody(IFormCollection formCollection) => new()
    {
        Query = formCollection.TryGetValue(QUERY_KEY, out var queryValues) ? queryValues[0] : null,
        Variables = formCollection.TryGetValue(VARIABLES_KEY, out var variablesValues) ? _serializer.Deserialize<Inputs>(variablesValues[0]) : null,
        Extensions = formCollection.TryGetValue(EXTENSIONS_KEY, out var extensionsValues) ? _serializer.Deserialize<Inputs>(extensionsValues[0]) : null,
        OperationName = formCollection.TryGetValue(OPERATION_NAME_KEY, out var operationNameValues) ? operationNameValues[0] : null,
        DocumentId = formCollection.TryGetValue(DOCUMENT_ID_KEY, out var documentIdValues) ? documentIdValues[0] : null,
    };

    /// <summary>
    /// Reads body of content type: application/graphql
    /// </summary>
    private static async Task<GraphQLRequest> DeserializeFromGraphBodyAsync(Stream bodyStream, System.Text.Encoding? encoding)
    {
        // do not close underlying HTTP connection
        using var streamReader = new StreamReader(bodyStream, encoding ?? System.Text.Encoding.UTF8, true, 1024, leaveOpen: true);

        // read query text
        string query = await streamReader.ReadToEndAsync();

        // return request; application/graphql MediaType supports only query text
        return new GraphQLRequest { Query = query };
    }

    private static bool TryGetEncoding(string? charset, out System.Text.Encoding? encoding)
    {
        encoding = null;

        if (string.IsNullOrEmpty(charset))
            return true;

        try
        {
            // Remove at most a single set of quotes.
            if (charset!.Length > 2 && charset[0] == '\"' && charset[charset.Length - 1] == '\"')
            {
                encoding = System.Text.Encoding.GetEncoding(charset.Substring(1, charset.Length - 2));
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
}
