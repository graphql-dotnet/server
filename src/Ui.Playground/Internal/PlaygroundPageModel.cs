using Newtonsoft.Json;
using System.IO;
using System.Reflection;
using System.Text;

namespace GraphQL.Server.Ui.Playground.Internal
{
    // https://docs.microsoft.com/en-us/aspnet/core/mvc/razor-pages/?tabs=netcore-cli
    internal class PlaygroundPageModel
    {
		private string _playgroundCSHtml;

		private readonly GraphQLPlaygroundOptions _options;

        public PlaygroundPageModel(GraphQLPlaygroundOptions options) => _options = options;

        public string Render()
        {
            if (_playgroundCSHtml == null)
            {
                var assembly = typeof(PlaygroundPageModel).GetTypeInfo().Assembly;

                using (var manifestResourceStream = assembly.GetManifestResourceStream("GraphQL.Server.Ui.Playground.Internal.playground.cshtml"))
                {
                    using (var streamReader = new StreamReader(manifestResourceStream))
                    {
                        var builder = new StringBuilder(streamReader.ReadToEnd());

                        builder.Replace("@Model.GraphQLEndPoint", _options.GraphQLEndPoint);
                        builder.Replace("@Model.GraphQLConfig", JsonConvert.SerializeObject(_options.GraphQLConfig));
                        builder.Replace("@Model.PlaygroundSettings", JsonConvert.SerializeObject(_options.PlaygroundSettings));

                        _playgroundCSHtml = builder.ToString();
                    }
                }
            }

            return _playgroundCSHtml;
		}
	}
}
