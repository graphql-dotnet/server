using Microsoft.AspNetCore.Mvc;

namespace GraphQL.Server.Ui.Playground;

/// <summary>
/// An action result that returns a Playground result
/// </summary>
public class PlaygroundActionResult : IActionResult
{
    private readonly PlaygroundMiddleware _playgroundMiddleware;

    /// <summary>
    /// Initializes the playground action result with the specified options.
    /// </summary>
    public PlaygroundActionResult(PlaygroundOptions options)
    {
        _playgroundMiddleware = new(_ => Task.CompletedTask, options);
    }

    /// <summary>
    /// Initializes the playground action result with the specified option configuration delegate.
    /// </summary>
    public PlaygroundActionResult(Action<PlaygroundOptions>? configure = null)
    {
        var options = new PlaygroundOptions();
        configure?.Invoke(options);
        _playgroundMiddleware = new(_ => Task.CompletedTask, options);
    }

    /// <inheritdoc/>
    public Task ExecuteResultAsync(ActionContext context)
        => _playgroundMiddleware.Invoke(context.HttpContext);
}
