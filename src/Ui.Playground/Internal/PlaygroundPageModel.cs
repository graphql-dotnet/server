using System.IO;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;

namespace GraphQL.Server.Ui.Playground.Internal {

	// https://docs.microsoft.com/en-us/aspnet/core/mvc/razor-pages/?tabs=netcore-cli
	internal class PlaygroundPageModel {

		private string playgroundCSHtml;

		private readonly GraphQLPlaygroundOptions options;

		public PlaygroundPageModel(GraphQLPlaygroundOptions options) {
			this.options = options;
		}

		public string Render() {
			if (playgroundCSHtml != null) {
				return playgroundCSHtml;
			}
			var assembly = typeof(PlaygroundPageModel).GetTypeInfo().Assembly;
            using (var manifestResourceStream = assembly.GetManifestResourceStream("GraphQL.Server.Ui.Playground.Internal.playground.cshtml")) {
                using (var streamReader = new StreamReader(manifestResourceStream)) {
                    var builder = new StringBuilder(streamReader.ReadToEnd());
                    builder.Replace("@Model.GraphQLEndPoint",
                        options.GraphQLEndPoint);
                    builder.Replace("@Model.GraphQLConfig",
                        JsonConvert.SerializeObject(options.GraphQLConfig));
                    builder.Replace("@Model.PlaygroundSettings",
                        JsonConvert.SerializeObject(options.PlaygroundSettings));
                    playgroundCSHtml = builder.ToString();
                    return this.Render();
                }
            }
		}

	}

}
