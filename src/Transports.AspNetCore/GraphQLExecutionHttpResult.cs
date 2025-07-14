#if NET6_0_OR_GREATER
namespace GraphQL.Server.Transports.AspNetCore;

/// <summary>
/// An <see cref="IResult"/> that executes a GraphQL request for the specified schema.
/// </summary>
public class GraphQLExecutionHttpResult<TSchema> : IResult
    where TSchema : ISchema
{
    private readonly GraphQLHttpMiddlewareOptions _options;

    /// <summary>
    /// Initializes a new instance with an optional middleware configuration delegate.
    /// </summary>
    public GraphQLExecutionHttpResult(Action<GraphQLHttpMiddlewareOptions>? configure = null)
        : this(Configure(configure))
    {
    }

    private static GraphQLHttpMiddlewareOptions Configure(Action<GraphQLHttpMiddlewareOptions>? configure)
    {
        var options = new GraphQLHttpMiddlewareOptions();
        configure?.Invoke(options);
        return options;
    }

    /// <summary>
    /// Initializes a new instance with the specified middleware options.
    /// </summary>
    public GraphQLExecutionHttpResult(GraphQLHttpMiddlewareOptions options)
    {
        _options = options;
    }

    /// <inheritdoc/>
    public virtual Task ExecuteAsync(HttpContext httpContext)
    {
        var provider = httpContext.RequestServices;
        var middleware = new GraphQLHttpMiddleware<TSchema>(
            static httpContext =>
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return Task.CompletedTask;
            },
            provider.GetRequiredService<IGraphQLTextSerializer>(),
            provider.GetRequiredService<IDocumentExecuter<TSchema>>(),
            provider.GetRequiredService<IServiceScopeFactory>(),
            _options,
            provider.GetService<IHostApplicationLifetime>() ?? NullHostApplicationLifetime.Instance);

        return middleware.InvokeAsync(httpContext);
    }
}

/// <summary>
/// An <see cref="IResult"/> that executes a GraphQL request for the default schema.
/// </summary>
public class GraphQLExecutionHttpResult : GraphQLExecutionHttpResult<ISchema>
{
    /// <inheritdoc cref="GraphQLExecutionHttpResult{TSchema}.GraphQLExecutionHttpResult(Action{GraphQLHttpMiddlewareOptions}?)"/>
    public GraphQLExecutionHttpResult(Action<GraphQLHttpMiddlewareOptions>? configure = null)
        : base(configure)
    {
    }

    /// <inheritdoc cref="GraphQLExecutionHttpResult{TSchema}.GraphQLExecutionHttpResult(GraphQLHttpMiddlewareOptions)"/>
    public GraphQLExecutionHttpResult(GraphQLHttpMiddlewareOptions options)
        : base(options)
    {
    }
}
#endif
