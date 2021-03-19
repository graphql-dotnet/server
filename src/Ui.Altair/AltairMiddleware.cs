using System;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Server.Ui.Altair.Internal;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Ui.Altair
{
    /// <summary>
    /// A middleware for Altair GraphQL UI.
    /// </summary>
    public class AltairMiddleware
    {
        private readonly AltairOptions _options;

        /// <summary>
        /// The page model used to render Altair.
        /// </summary>
        private AltairPageModel _pageModel;

        /// <summary>
        /// Create a new <see cref="AltairMiddleware"/>
        /// </summary>
        /// <param name="next">The next request delegate; not used, this is a terminal middleware.</param>
        /// <param name="options">Options to customize middleware</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "ASP.NET Core conventions")]
        public AltairMiddleware(RequestDelegate next, AltairOptions options)
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

            // Initialize page model if null
            if (_pageModel == null)
                _pageModel = new AltairPageModel(_options);

            byte[] data = Encoding.UTF8.GetBytes(_pageModel.Render());
            return httpContext.Response.Body.WriteAsync(data, 0, data.Length);
        }
    }
}
