namespace GraphQL.Server.Transports.AspNetCore.AzureFunctions;

/// <summary>
/// An <see cref="IActionResult"/> that executes a GraphQL request for the specified schema.
/// </summary>
public class AzureGraphQLActionResult<TSchema> : IActionResult
    where TSchema : ISchema
{
    private readonly HttpRequest _httpRequest;
    private readonly IAzureGraphQLMiddleware<TSchema> _middleware;

    /// <summary>
    /// Initializes a new instance with the specified <see cref="HttpRequest"/>.
    /// </summary>
    /// <param name="httpRequest"></param>
    public AzureGraphQLActionResult(HttpRequest httpRequest)
    {
        _httpRequest = httpRequest;
        _middleware = httpRequest.HttpContext.RequestServices.GetRequiredService<IAzureGraphQLMiddleware<TSchema>>();
    }

    /// <inheritdoc/>
    public virtual Task ExecuteResultAsync(ActionContext context)
        => _middleware.InvokeAsync(_httpRequest, static httpContext =>
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
            return Task.CompletedTask;
        });
}

/// <summary>
/// An <see cref="IActionResult"/> that executes a GraphQL request for the default schema.
/// </summary>
public class AzureGraphQLActionResult : AzureGraphQLActionResult<ISchema>
{
    /// <inheritdoc cref="AzureGraphQLActionResult{TSchema}.AzureGraphQLActionResult(HttpRequest)"/>
    public AzureGraphQLActionResult(HttpRequest httpRequest)
        : base(httpRequest)
    {
    }
}
