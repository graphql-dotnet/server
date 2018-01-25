using System.IO;
using System.Reflection;
using System.Text;

namespace GraphQL.Server.AspNetCore.GraphiQL.Internal {

	// https://docs.microsoft.com/en-us/aspnet/core/mvc/razor-pages/?tabs=netcore-cli
	internal class GraphiQLPageModel {

		private static string graphiQLCSHtml;

		private readonly GraphiQLMiddlewareSettings settings;

		public GraphiQLPageModel(GraphiQLMiddlewareSettings settings) {
			this.settings = settings;
		}

		public string Render() {
			if (graphiQLCSHtml != null) {
				return graphiQLCSHtml;
			}
			var assembly = typeof(GraphiQLPageModel).GetTypeInfo().Assembly;
			var resource = assembly.GetManifestResourceStream("GraphQL.Server.AspNetCore.GraphiQL.Internal.graphiql.cshtml");

			var builder = new StringBuilder(new StreamReader(resource).ReadToEnd());
			builder.Replace("@Model.GraphQLEndPoint", this.settings.GraphQLEndPoint);
			graphiQLCSHtml = builder.ToString();

			return this.Render();
		}

	}

}
