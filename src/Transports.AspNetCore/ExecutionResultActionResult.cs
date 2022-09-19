namespace GraphQL.Server.Transports.AspNetCore;

/// <summary>
/// An action result that formats the <see cref="ExecutionResult"/> as JSON.
/// </summary>
public sealed class ExecutionResultActionResult : IActionResult
{
    private readonly ExecutionResult _executionResult;
    private readonly HttpStatusCode _statusCode;

    /// <inheritdoc cref="ExecutionResultActionResult"/>
    public ExecutionResultActionResult(ExecutionResult executionResult)
    {
        _executionResult = executionResult;
        _statusCode = executionResult.Executed ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
    }

    /// <inheritdoc cref="ExecutionResultActionResult"/>
    public ExecutionResultActionResult(ExecutionResult executionResult, HttpStatusCode statusCode)
    {
        _executionResult = executionResult;
        _statusCode = statusCode;
    }

    /// <inheritdoc cref="HttpResponse.ContentType"/>
    public string ContentType { get; set; } = GraphQLHttpMiddleware.CONTENTTYPE_GRAPHQLRESPONSEJSON;

    /// <inheritdoc/>
    public async Task ExecuteResultAsync(ActionContext context)
    {
        var serializer = context.HttpContext.RequestServices.GetRequiredService<IGraphQLSerializer>();
        var response = context.HttpContext.Response;
        response.ContentType = ContentType;
        response.StatusCode = (int)_statusCode;
        await serializer.WriteAsync(response.Body, _executionResult, context.HttpContext.RequestAborted);
    }
}
