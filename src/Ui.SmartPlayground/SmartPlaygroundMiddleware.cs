using GraphQL.Server.Ui.SmartPlayground.Internal;
using System.Text;
using GraphQL.Server.Ui.SmartPlayground.Factories;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Ui.SmartPlayground;

/// <summary>
/// A middleware for GraphQL Playground UI.
/// </summary>
public class SmartPlaygroundMiddleware
{
    private readonly SmartPlaygroundOptions _options;
    private readonly ISmartClientFactory _smartClientFactory;

    private PlaygroundPageModel? _pageModel;

    public SmartPlaygroundMiddleware(RequestDelegate _, SmartPlaygroundOptions options, ISmartClientFactory smartClientFactory)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _smartClientFactory = smartClientFactory ?? throw new ArgumentNullException(nameof(smartClientFactory));
    }

    public async Task Launch()
    {
        var smartClient = _smartClientFactory.CreateClient(_options);
        await smartClient.Launch();
    }

    public async Task<string> Redirect(string code)
    {
        var smartClient = _smartClientFactory.CreateClient(_options);
        return await smartClient.Redirect(code);
    }

    /// <summary>
    /// Try to execute the logic of the middleware
    /// </summary>
    /// <param name="httpContext">The HttpContext</param>
    public async Task Invoke(HttpContext httpContext)
    {
        if (httpContext == null)
        {
            throw new ArgumentNullException(nameof(httpContext));
        }

        if (httpContext.Request.Path.StartsWithSegments(new PathString("/ui/smartplayground/launch")))
        {
            // Launch endpoint - reset and start new launch
            if (httpContext.Request.Cookies["token"] != null)
            {
                httpContext.Response.Cookies.Delete("token");
            }

            await Launch();
        }
        else
        {
            string? token = null;

            if (httpContext.Request.Query.ContainsKey("code"))
            {
                token = await Redirect(httpContext.Request.Query["code"]);
            }
            else if (httpContext.Request.Cookies["token"] != null)
            {
                token = httpContext.Request.Cookies["token"];
            }

            if (!string.IsNullOrEmpty(token))
            {
                _options.Headers = new Dictionary<string, object> { { "Authorization", $"Bearer {token}" } };
                _pageModel ??= new PlaygroundPageModel(_options);

                byte[] data = Encoding.UTF8.GetBytes(_pageModel.Render());

                httpContext.Request.QueryString = new QueryString();
                _ = httpContext.Response.Body.WriteAsync(data, 0, data.Length);
            }
        }
    }
}
