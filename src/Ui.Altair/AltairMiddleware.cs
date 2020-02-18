using GraphQL.Server.Ui.Altair.Internal;
using Microsoft.AspNetCore.Http;
using System;
using System.Text;
using System.Threading.Tasks;

namespace GraphQL.Server.Ui.Altair
{
    /// <summary>
    /// A middleware for Altair GraphQL
    /// </summary>
    public class AltairMiddleware
    {
        private readonly GraphQLAltairOptions _options;

        /// <summary>
        /// The next middleware
        /// </summary>
        private readonly RequestDelegate _nextMiddleware;

        /// <summary>
        /// The page model used to render Altair
        /// </summary>
        private AltairPageModel _pageModel;

        /// <summary>
        /// Create a new <see cref="AltairMiddleware"/>
        /// </summary>
        /// <param name="nextMiddleware">The next middleware</param>
        /// <param name="options">Options to customize middleware</param>
        public AltairMiddleware(RequestDelegate nextMiddleware, GraphQLAltairOptions options)
        {
            _nextMiddleware = nextMiddleware ?? throw new ArgumentNullException(nameof(nextMiddleware));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Try to execute the logic of the middleware
        /// </summary>
        /// <param name="httpContext">The HttpContext</param>
        public Task Invoke(HttpContext httpContext)
        {
            if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));

            return IsAltairRequest(httpContext.Request)
                ? InvokeAltair(httpContext.Response)
                : _nextMiddleware(httpContext);
        }

        private bool IsAltairRequest(HttpRequest httpRequest)
        {
            return HttpMethods.IsGet(httpRequest.Method) && httpRequest.Path.StartsWithSegments(_options.Path);
        }

        private Task InvokeAltair(HttpResponse httpResponse)
        {
            httpResponse.ContentType = "text/html";
            httpResponse.StatusCode = 200;

            // Initialize page model if null
            if (_pageModel == null)
                _pageModel = new AltairPageModel(_options);

            var data = Encoding.UTF8.GetBytes(_pageModel.Render());
            return httpResponse.Body.WriteAsync(data, 0, data.Length);
        }
    }
}
