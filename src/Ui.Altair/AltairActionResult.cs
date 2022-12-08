using Microsoft.AspNetCore.Mvc;

namespace GraphQL.Server.Ui.Altair;

/// <summary>
/// An action result that returns a Altair user interface.
/// </summary>
public class AltairActionResult : IActionResult
{
    private readonly AltairMiddleware _middleware;

    /// <summary>
    /// Initializes the Altair action result with the specified options.
    /// </summary>
    public AltairActionResult(AltairOptions options)
    {
        _middleware = new(_ => Task.CompletedTask, options);
    }

    /// <summary>
    /// Initializes the Altair action result with the specified optional configuration delegate.
    /// </summary>
    public AltairActionResult(Action<AltairOptions>? configure = null)
    {
        var options = new AltairOptions();
        configure?.Invoke(options);
        _middleware = new(_ => Task.CompletedTask, options);
    }

    /// <inheritdoc/>
    public Task ExecuteResultAsync(ActionContext context)
        => _middleware.Invoke(context.HttpContext);
}
