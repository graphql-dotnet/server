using GraphQL.Server.Ui.GraphiQL.Internal;
using Microsoft.AspNetCore.Http;
using System;
using System.Text;
using System.Threading.Tasks;

namespace GraphQL.Server.Ui.GraphiQL
{
    /// <summary>
    /// A middleware for GraphiQL
    /// </summary>
    public class GraphiQLMiddleware
    {
        private readonly GraphiQLOptions _options;

        /// <summary>
        /// The next middleware
        /// </summary>
        private readonly RequestDelegate _nextMiddleware;

        /// <summary>
        /// The page model used to render GraphiQL
        /// </summary>
        private GraphiQLPageModel _pageModel;

        /// <summary>
        /// Create a new <see cref="GraphiQLMiddleware"/>
        /// </summary>
        /// <param name="nextMiddleware">The next middleware</param>
        /// <param name="options">Options to customize middleware</param>
        public GraphiQLMiddleware(RequestDelegate nextMiddleware, GraphiQLOptions options)
        {
            _nextMiddleware = nextMiddleware ?? throw new ArgumentNullException(nameof(nextMiddleware));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Try to execute the logic of the middleware
        /// </summary>
        /// <param name="httpContext">The HttpContext</param>
        /// <returns></returns>
        public Task Invoke(HttpContext httpContext)
        {
            if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));

            return IsGraphiQLRequest(httpContext.Request)
                ? InvokeGraphiQL(httpContext.Response)
                : _nextMiddleware(httpContext);
        }

        private bool IsGraphiQLRequest(HttpRequest httpRequest)
        {
            return HttpMethods.IsGet(httpRequest.Method) && httpRequest.Path.StartsWithSegments(_options.Path);
        }

        private Task InvokeGraphiQL(HttpResponse httpResponse)
        {
            httpResponse.ContentType = "text/html";
            httpResponse.StatusCode = 200;

            // Initialize page model if null
            if (_pageModel == null)
                _pageModel = new GraphiQLPageModel(_options);

            var data = Encoding.UTF8.GetBytes(_pageModel.Render());
            return httpResponse.Body.WriteAsync(data, 0, data.Length);
        }
    }
}
