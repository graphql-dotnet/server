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
        private readonly GraphQLVoyagerOptions _options;

        /// <summary>
        /// The next middleware
        /// </summary>
        private readonly RequestDelegate _nextMiddleware;

        /// <summary>
        /// The page model used to render Voyager
        /// </summary>
        private VoyagerPageModel _pageModel;

        /// <summary>
        /// Create a new <see cref="VoyagerMiddleware"/>
        /// </summary>
        /// <param name="nextMiddleware">The next middleware</param>
        /// <param name="options">Options to customize middleware</param>
        public VoyagerMiddleware(RequestDelegate nextMiddleware, GraphQLVoyagerOptions options)
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

            return IsVoyagerRequest(httpContext.Request)
                ? InvokeVoyager(httpContext.Response)
                : _nextMiddleware(httpContext);
        }

        private bool IsVoyagerRequest(HttpRequest httpRequest)
            => HttpMethods.IsGet(httpRequest.Method) && httpRequest.Path.StartsWithSegments(_options.Path);

        private Task InvokeVoyager(HttpResponse httpResponse)
        {
            httpResponse.ContentType = "text/html";
            httpResponse.StatusCode = 200;

            // Initialize page model if null
            if (_pageModel == null)
                _pageModel = new VoyagerPageModel(_options);

            byte[] data = Encoding.UTF8.GetBytes(_pageModel.Render());
            return httpResponse.Body.WriteAsync(data, 0, data.Length);
        }
    }
}
