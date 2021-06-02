using System;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Server.Ui.GraphiQL.Internal;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Ui.GraphiQL
{
    /// <summary>
    /// A middleware for GraphiQL UI.
    /// </summary>
    public class GraphiQLMiddleware
    {
        private readonly GraphiQLOptions _options;

        /// <summary>
        /// The page model used to render GraphiQL.
        /// </summary>
        private GraphiQLPageModel _pageModel;

        /// <summary>
        /// Create a new <see cref="GraphiQLMiddleware"/>
        /// </summary>
        /// <param name="next">The next request delegate; not used, this is a terminal middleware.</param>
        /// <param name="options">Options to customize middleware</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "ASP.NET Core conventions")]
        public GraphiQLMiddleware(RequestDelegate next, GraphiQLOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Try to execute the logic of the middleware
        /// </summary>
        /// <param name="httpContext">The HttpContext</param>
        /// <returns></returns>
        public Task Invoke(HttpContext httpContext)
        {
            if (httpContext == null)
                throw new ArgumentNullException(nameof(httpContext));

            httpContext.Response.ContentType = "text/html";
            httpContext.Response.StatusCode = 200;

            // Initialize page model if null
            if (_pageModel == null)
                _pageModel = new GraphiQLPageModel(_options);

            byte[] data = Encoding.UTF8.GetBytes(_pageModel.Render());
            return httpContext.Response.Body.WriteAsync(data, 0, data.Length);
        }
    }
}
