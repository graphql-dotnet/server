#if NET6_0_OR_GREATER
namespace GraphQL.Server.Transports.AspNetCore;

/// <summary>
/// An <see cref="IResult"/> that formats the <see cref="ExecutionResult"/> as JSON.
/// </summary>
public sealed class ExecutionResultHttpResult : IResult
{
    private readonly ExecutionResult _executionResult;
    private readonly HttpStatusCode _statusCode;

    /// <inheritdoc cref="ExecutionResultHttpResult"/>
    public ExecutionResultHttpResult(ExecutionResult executionResult)
    {
        _executionResult = executionResult;
        _statusCode = executionResult.Executed ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
    }

    /// <inheritdoc cref="ExecutionResultHttpResult"/>
    public ExecutionResultHttpResult(ExecutionResult executionResult, HttpStatusCode statusCode)
    {
        _executionResult = executionResult;
        _statusCode = statusCode;
    }

    /// <inheritdoc cref="HttpResponse.ContentType"/>
    public string ContentType { get; set; } = GraphQLHttpMiddleware.CONTENTTYPE_GRAPHQLRESPONSEJSON;

    /// <inheritdoc/>
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var serializer = httpContext.RequestServices.GetRequiredService<IGraphQLSerializer>();
        var response = httpContext.Response;
        response.ContentType = ContentType;
        response.StatusCode = (int)_statusCode;
        await serializer.WriteAsync(response.Body, _executionResult, httpContext.RequestAborted);
    }
}
#endif
