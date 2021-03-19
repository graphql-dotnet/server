using System;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Server.Ui.Voyager.Internal;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Ui.Voyager
{
    /// <summary>
    /// A middleware for Voyager UI.
    /// </summary>
    public class VoyagerMiddleware
    {
        private readonly VoyagerOptions _options;

        /// <summary>
        /// The page model used to render Voyager.
        /// </summary>
        private VoyagerPageModel _pageModel;

        /// <summary>
        /// Create a new <see cref="VoyagerMiddleware"/>
        /// </summary>
        /// <param name="next">The next request delegate; not used, this is a terminal middleware.</param>
        /// <param name="options">Options to customize middleware</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "ASP.NET Core conventions")]
        public VoyagerMiddleware(RequestDelegate next, VoyagerOptions options)
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
                _pageModel = new VoyagerPageModel(_options);

            byte[] data = Encoding.UTF8.GetBytes(_pageModel.Render());
            return httpContext.Response.Body.WriteAsync(data, 0, data.Length);
        }
    }
}
