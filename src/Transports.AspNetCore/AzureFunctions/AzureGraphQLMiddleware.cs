#pragma warning disable CA1716 // Identifiers should not match keywords

namespace GraphQL.Server.Transports.AspNetCore.AzureFunctions;

/// <summary>
/// Middleware logic for Azure Functions.
/// </summary>
public class AzureGraphQLMiddleware<TSchema> : GraphQLHttpMiddleware<TSchema>, IAzureGraphQLMiddleware<TSchema>
    where TSchema : ISchema
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzureGraphQLMiddleware{TSchema}"/> class.
    /// </summary>
    public AzureGraphQLMiddleware(
        IGraphQLTextSerializer serializer,
        IDocumentExecuter<TSchema> documentExecuter,
        IServiceScopeFactory serviceScopeFactory,
        GraphQLHttpMiddlewareOptions options)
        : base(_ => Task.CompletedTask, serializer, documentExecuter, serviceScopeFactory, options, NullHostApplicationLifetime.Instance)
    {
    }

    /// <inheritdoc/>
    public override Task InvokeAsync(HttpContext context)
        => InvokeAsync(context.Request, static httpContext =>
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
            return Task.CompletedTask;
        });

    /// <inheritdoc cref="GraphQLHttpMiddleware.InvokeAsync(HttpContext, RequestDelegate)"/>
    public virtual Task InvokeAsync(HttpRequest request, RequestDelegate next)
        => base.InvokeAsync(request.HttpContext, next);

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
