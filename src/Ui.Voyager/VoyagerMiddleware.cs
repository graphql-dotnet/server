using GraphQL.Server.Ui.Voyager.Internal;
using Microsoft.AspNetCore.Http;
using System;
using System.Text;
using System.Threading.Tasks;

namespace GraphQL.Server.Ui.Voyager
{
    /// <summary>
    /// A middleware for Voyager
    /// </summary>
    public class VoyagerMiddleware
    {
        private readonly GraphQLVoyagerOptions _settings;

        /// <summary>
        /// The Next Middleware
        /// </summary>
        private readonly RequestDelegate _nextMiddleware;

        /// <summary>
        /// The page model used to render Voyager
        /// </summary>
        private VoyagerPageModel _pageModel;

        /// <summary>
        /// Create a new VoyagerMiddleware
        /// </summary>
        /// <param name="nextMiddleware">The Next Middleware</param>
        /// <param name="settings">The Settings of the Middleware</param>
        public VoyagerMiddleware(RequestDelegate nextMiddleware, GraphQLVoyagerOptions settings)
        {
            _nextMiddleware = nextMiddleware ?? throw new ArgumentNullException(nameof(nextMiddleware));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Try to execute the logic of the middleware
        /// </summary>
        /// <param name="httpContext">The HttpContext</param>
        /// <returns></returns>
        public Task Invoke(HttpContext httpContext)
        {
            if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));

            return IsVoyagerRequest(httpContext.Request)
                ? InvokeVoyager(httpContext.Response)
                : _nextMiddleware(httpContext);
        }

        private bool IsVoyagerRequest(HttpRequest httpRequest)
        {
            return HttpMethods.IsGet(httpRequest.Method) && httpRequest.Path.StartsWithSegments(_settings.Path);
        }

        private Task InvokeVoyager(HttpResponse httpResponse)
        {
            httpResponse.ContentType = "text/html";
            httpResponse.StatusCode = 200;

            // Initialize page model if null
            if (_pageModel == null)
                _pageModel = new VoyagerPageModel(_settings);

            var data = Encoding.UTF8.GetBytes(_pageModel.Render());
            return httpResponse.Body.WriteAsync(data, 0, data.Length);
        }
    }
}
