using Microsoft.AspNetCore.Mvc;

namespace GraphQL.Server.Ui.Voyager;

/// <summary>
/// An action result that returns a Voyager user interface.
/// </summary>
public class VoyagerActionResult : IActionResult
{
    private readonly VoyagerMiddleware _middleware;

    /// <summary>
    /// Initializes the Voyager action result with the specified options.
    /// </summary>
    public VoyagerActionResult(VoyagerOptions options)
    {
        _middleware = new(_ => Task.CompletedTask, options);
    }

    /// <summary>
    /// Initializes the Voyager action result with the specified optional configuration delegate.
    /// </summary>
    public VoyagerActionResult(Action<VoyagerOptions>? configure = null)
    {
        var options = new VoyagerOptions();
        configure?.Invoke(options);
        _middleware = new(_ => Task.CompletedTask, options);
    }

    /// <inheritdoc/>
    public Task ExecuteResultAsync(ActionContext context)
        => _middleware.Invoke(context.HttpContext);
}
