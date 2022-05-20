#nullable enable

using System.Diagnostics;
using GraphQL.Server.Transports.AspNetCore;
using GraphQL.Transport;
using GraphQL.Types;

namespace GraphQL.Samples.Server;

// Example of a custom GraphQL Middleware that sends execution result to Microsoft.Extensions.Logging API
public class GraphQLHttpMiddlewareWithLogs<TSchema> : GraphQLHttpMiddleware<TSchema>
    where TSchema : ISchema
{
    private readonly ILogger _logger;

    public GraphQLHttpMiddlewareWithLogs(
        RequestDelegate next,
        IGraphQLTextSerializer serializer,
        IDocumentExecuter<TSchema> documentExecuter,
        IServiceScopeFactory serviceScopeFactory,
        GraphQLHttpMiddlewareOptions options,
        ILogger<GraphQLHttpMiddleware<TSchema>> logger)
        : base(next, serializer, documentExecuter, serviceScopeFactory, options)
    {
        _logger = logger;
    }

    protected override async Task<ExecutionResult> ExecuteRequestAsync(HttpContext context, GraphQLRequest? request, IServiceProvider serviceProvider, IDictionary<string, object?> userContext)
    {
        var timer = Stopwatch.StartNew();
        var ret = await base.ExecuteRequestAsync(context, request, serviceProvider, userContext);
        if (ret.Errors != null)
        {
            _logger.LogError("GraphQL execution completed in {Elapsed} with error(s): {Errors}", timer.Elapsed, ret.Errors);
        }
        else
            _logger.LogInformation("GraphQL execution successfully completed in {Elapsed}", timer.Elapsed);

        return ret;
    }
}
