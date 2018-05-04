using System;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Server.Ui.GraphiQL.Internal;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Ui.GraphiQL {

    /// <summary>
    /// A middleware for GraphiQL
    /// </summary>
    public class GraphiQLMiddleware {

		private readonly GraphiQLOptions settings;

        /// <summary>
        /// The Next Middleware
        /// </summary>
        private readonly RequestDelegate nextMiddleware;
        
        /// <summary>
        /// The page model used to render GraphiQL
        /// </summary>
        private GraphiQLPageModel _pageModel;
        
        /// <summary>
        /// Create a new GraphiQLMiddleware
        /// </summary>
        /// <param name="nextMiddleware">The Next Middleware</param>
        /// <param name="settings">The Settings of the Middleware</param>
        public GraphiQLMiddleware(RequestDelegate nextMiddleware, GraphiQLOptions settings) {
            this.nextMiddleware = nextMiddleware ?? throw new ArgumentNullException(nameof(nextMiddleware));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
		}

		/// <summary>
		/// Try to execute the logic of the middleware
		/// </summary>
		/// <param name="httpContext">The HttpContext</param>
		/// <returns></returns>
		public async Task Invoke(HttpContext httpContext) {
			if (httpContext == null) { throw new ArgumentNullException(nameof(httpContext)); }

			if (this.IsGraphiQLRequest(httpContext.Request)) {
				await this.InvokeGraphiQL(httpContext.Response).ConfigureAwait(false);
				return;
			}

			await this.nextMiddleware(httpContext).ConfigureAwait(false);
		}

		private bool IsGraphiQLRequest(HttpRequest httpRequest) {
			return httpRequest.Path.StartsWithSegments(this.settings.GraphiQLPath)
				&& string.Equals(httpRequest.Method, HttpMethods.Get, StringComparison.OrdinalIgnoreCase);
		}

		private async Task InvokeGraphiQL(HttpResponse httpResponse) {
			httpResponse.ContentType = "text/html";
			httpResponse.StatusCode = 200;

		    // Initilize page model if null
		    if (_pageModel == null)
		        _pageModel = new GraphiQLPageModel(this.settings);

            var data = Encoding.UTF8.GetBytes(_pageModel.Render());
			await httpResponse.Body.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
		}

	}

}
