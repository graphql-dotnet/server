using System;
using GraphQL.Server.Ui.GraphiQL;

namespace Microsoft.AspNetCore.Builder {

    /// <summary>
    /// Extension methods for <see cref="GraphiQLMiddleware"/>
    /// </summary>
    public static class GraphiQLMiddlewareExtensions {

		/// <summary>
		/// Enables a GraphiQLServer using the specified settings
		/// </summary>
		/// <param name="applicationBuilder"></param>
		/// <param name="settings">The settings of the Middleware</param>
		/// <returns></returns>
		public static IApplicationBuilder UseGraphiQLServer(this IApplicationBuilder applicationBuilder, GraphiQLOptions settings) {
			if (settings == null) { throw new ArgumentNullException(nameof(settings)); }

			applicationBuilder.UseMiddleware<GraphiQLMiddleware>(settings);
			return applicationBuilder;
		}

	}

}
