using System;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Server.AspNetCore.GraphiQL.Internal;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.AspNetCore.GraphiQL {

	/// <summary>
	/// A middleware for GraphiQL
	/// </summary>
	public class GraphiQLMiddleware : BaseMiddleware {

		private readonly GraphiQLMiddlewareSettings settings;

		/// <summary>
		/// Create a new GraphiQLMiddleware
		/// </summary>
		/// <param name="nextMiddleware">The Next Middleware</param>
		/// <param name="settings">The Settings of the Middleware</param>
		public GraphiQLMiddleware(RequestDelegate nextMiddleware, GraphiQLMiddlewareSettings settings) : base(nextMiddleware) {
			this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
		}

		/// <summary>
		/// Try to execute the logic of the middleware
		/// </summary>
		/// <param name="httpContext">The HttpContext</param>
		/// <returns></returns>
		public async override Task Invoke(HttpContext httpContext) {
			if (httpContext == null) { throw new ArgumentNullException(nameof(httpContext)); }

			if (this.IsGraphiQLRequest(httpContext.Request)) {
				await this.InvokeGraphiQL(httpContext.Response).ConfigureAwait(false);
				return;
			}

			await this.NextMiddleware(httpContext).ConfigureAwait(false);
		}

		private bool IsGraphiQLRequest(HttpRequest httpRequest) {
			return httpRequest.Path.StartsWithSegments(this.settings.GraphiQLPath)
				&& string.Equals(httpRequest.Method, HttpMethods.Get, StringComparison.OrdinalIgnoreCase);
		}

		private async Task InvokeGraphiQL(HttpResponse httpResponse) {
			httpResponse.ContentType = "text/html";
			httpResponse.StatusCode = 200;

			// TODO: use RazorPageGenerator when ASP.NET Core 1.1 is out...?
			var graphiQLPageModel = new GraphiQLPageModel(this.settings);

			var data = Encoding.UTF8.GetBytes(graphiQLPageModel.Render());
			await httpResponse.Body.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
		}

	}

}
