using GraphQL.Server.Ui.Playground.Internal;
using Microsoft.AspNetCore.Http;
using System;
using System.Text;
using System.Threading.Tasks;

namespace GraphQL.Server.Ui.Playground
{
    /// <summary>
    /// A middleware for Playground
    /// </summary>
    public class PlaygroundMiddleware
    {
        private readonly GraphQLPlaygroundOptions _settings;

        /// <summary>
        /// The Next Middleware
        /// </summary>
        private readonly RequestDelegate _nextMiddleware;

        /// <summary>
        /// The page model used to render Playground
        /// </summary>
        private PlaygroundPageModel _pageModel;

        /// <summary>
        /// Create a new PlaygroundMiddleware
        /// </summary>
        /// <param name="nextMiddleware">The Next Middleware</param>
        /// <param name="settings">The Settings of the Middleware</param>
        public PlaygroundMiddleware(RequestDelegate nextMiddleware, GraphQLPlaygroundOptions settings)
        {
            _nextMiddleware = nextMiddleware ?? throw new ArgumentNullException(nameof(nextMiddleware));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Try to execute the logic of the middleware
        /// </summary>
        /// <param name="httpContext">The HttpContext</param>
        public Task Invoke(HttpContext httpContext)
        {
            if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));

            return IsPlaygroundRequest(httpContext.Request)
                ? InvokePlayground(httpContext.Response)
                : _nextMiddleware(httpContext);
        }

        private bool IsPlaygroundRequest(HttpRequest httpRequest)
        {
            return HttpMethods.IsGet(httpRequest.Method) && httpRequest.Path.StartsWithSegments(_settings.Path);
        }

        private Task InvokePlayground(HttpResponse httpResponse)
        {
            httpResponse.ContentType = "text/html";
            httpResponse.StatusCode = 200;

            // Initialize page model if null
            if (_pageModel == null)
                _pageModel = new PlaygroundPageModel(_settings);

            var data = Encoding.UTF8.GetBytes(_pageModel.Render());
            return httpResponse.Body.WriteAsync(data, 0, data.Length);
        }
    }
}
