namespace GraphQL.Server.Transports.AspNetCore;

/// <summary>
/// An <see cref="IActionResult"/> that executes a GraphQL request for the specified schema.
/// </summary>
public class GraphQLExecutionActionResult<TSchema> : IActionResult
    where TSchema : ISchema
{
    private readonly GraphQLHttpMiddlewareOptions _options;

    /// <summary>
    /// Initializes a new instance with an optional middleware configuration delegate.
    /// </summary>
    public GraphQLExecutionActionResult(Action<GraphQLHttpMiddlewareOptions>? configure = null)
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
    public GraphQLExecutionActionResult(GraphQLHttpMiddlewareOptions options)
    {
        _options = options;
    }

    /// <inheritdoc/>
    public virtual Task ExecuteResultAsync(ActionContext context)
    {
        var provider = context.HttpContext.RequestServices;
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

        return middleware.InvokeAsync(context.HttpContext);
    }

    private class NullHostApplicationLifetime : IHostApplicationLifetime
    {
        private NullHostApplicationLifetime()
        {
        }

        public static NullHostApplicationLifetime Instance { get; } = new();

        public CancellationToken ApplicationStarted => default;

        public CancellationToken ApplicationStopped => default;

        public CancellationToken ApplicationStopping => default;

        public void StopApplication() { }
    }
}

/// <summary>
/// An <see cref="IActionResult"/> that executes a GraphQL request for the default schema.
/// </summary>
public class GraphQLExecutionActionResult : GraphQLExecutionActionResult<ISchema>
{
    /// <inheritdoc cref="GraphQLExecutionActionResult{TSchema}.GraphQLExecutionActionResult(Action{GraphQLHttpMiddlewareOptions}?)"/>
    public GraphQLExecutionActionResult(Action<GraphQLHttpMiddlewareOptions>? configure = null)
        : base(configure)
    {
    }

    /// <inheritdoc cref="GraphQLExecutionActionResult{TSchema}.GraphQLExecutionActionResult(GraphQLHttpMiddlewareOptions)"/>
    public GraphQLExecutionActionResult(GraphQLHttpMiddlewareOptions options)
        : base(options)
    {
    }
}
