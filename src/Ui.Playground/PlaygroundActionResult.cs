using Microsoft.AspNetCore.Mvc;

namespace GraphQL.Server.Ui.Playground;

/// <summary>
/// An action result that returns a Playground user interface.
/// </summary>
[Obsolete("GraphQL Playground has not been updated since 2019. Please use GraphiQL instead.")]
public class PlaygroundActionResult : IActionResult
{
    private readonly PlaygroundMiddleware _middleware;

    /// <summary>
    /// Initializes the Playground action result with the specified options.
    /// </summary>
    public PlaygroundActionResult(PlaygroundOptions options)
    {
        _middleware = new(_ => Task.CompletedTask, options);
    }

    /// <summary>
    /// Initializes the Playground action result with the specified optional configuration delegate.
    /// </summary>
    public PlaygroundActionResult(Action<PlaygroundOptions>? configure = null)
    {
        var options = new PlaygroundOptions();
        configure?.Invoke(options);
        _middleware = new(_ => Task.CompletedTask, options);
    }

    /// <inheritdoc/>
    public Task ExecuteResultAsync(ActionContext context)
        => _middleware.Invoke(context.HttpContext);
}
