using Microsoft.AspNetCore.Mvc;

namespace GraphQL.Server.Ui.GraphiQL;

/// <summary>
/// An action result that returns a GraphiQL user interface.
/// </summary>
public class GraphiQLActionResult : IActionResult
{
    private readonly GraphiQLMiddleware _middleware;

    /// <summary>
    /// Initializes the GraphiQL action result with the specified options.
    /// </summary>
    public GraphiQLActionResult(GraphiQLOptions options)
    {
        _middleware = new(_ => Task.CompletedTask, options);
    }

    /// <summary>
    /// Initializes the GraphiQL action result with the specified optional configuration delegate.
    /// </summary>
    public GraphiQLActionResult(Action<GraphiQLOptions>? configure = null)
    {
        var options = new GraphiQLOptions();
        configure?.Invoke(options);
        _middleware = new(_ => Task.CompletedTask, options);
    }

    /// <inheritdoc/>
    public Task ExecuteResultAsync(ActionContext context)
        => _middleware.Invoke(context.HttpContext);
}
