using System.Text;
using GraphQL.Server.Ui.Playground.Internal;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Ui.Playground;

/// <summary>
/// A middleware for GraphQL Playground UI.
/// </summary>
public class PlaygroundMiddleware
{
    private readonly PlaygroundOptions _options;

    /// <summary>
    /// The page model used to render Playground.
    /// </summary>
    private PlaygroundPageModel? _pageModel;

    /// <summary>
    /// Create a new <see cref="PlaygroundMiddleware"/>
    /// </summary>
    /// <param name="next">The next request delegate; not used, this is a terminal middleware.</param>
    /// <param name="options">Options to customize middleware</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "ASP.NET Core conventions")]
    public PlaygroundMiddleware(RequestDelegate next, PlaygroundOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Try to execute the logic of the middleware
    /// </summary>
    /// <param name="httpContext">The HttpContext</param>
    public Task Invoke(HttpContext httpContext)
    {
        if (httpContext == null)
            throw new ArgumentNullException(nameof(httpContext));

        httpContext.Response.ContentType = "text/html";
        httpContext.Response.StatusCode = 200;

        _pageModel ??= new PlaygroundPageModel(_options);

        byte[] data = Encoding.UTF8.GetBytes(_pageModel.Render());
        return httpContext.Response.Body.WriteAsync(data, 0, data.Length);
    }
}
