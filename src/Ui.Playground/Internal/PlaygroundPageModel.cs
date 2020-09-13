using System.IO;
using System.Text;

namespace GraphQL.Server.Ui.Playground.Internal
{
    // https://docs.microsoft.com/en-us/aspnet/core/mvc/razor-pages/?tabs=netcore-cli
    internal class PlaygroundPageModel
    {
        private string _playgroundCSHtml;

        private readonly GraphQLPlaygroundOptions _options;

        public PlaygroundPageModel(GraphQLPlaygroundOptions options)
        {
            _options = options;
        }

        public string Render()
        {
            if (_playgroundCSHtml == null)
            {
                using var manifestResourceStream = typeof(PlaygroundPageModel).Assembly.GetManifestResourceStream("GraphQL.Server.Ui.Playground.Internal.playground.cshtml");
                using var streamReader = new StreamReader(manifestResourceStream);

                var builder = new StringBuilder(streamReader.ReadToEnd())
                    .Replace("@Model.GraphQLEndPoint", _options.GraphQLEndPoint)
                    .Replace("@Model.GraphQLConfig", Serializer.Serialize(_options.GraphQLConfig))
                    .Replace("@Model.Headers", Serializer.Serialize(_options.Headers))
                    .Replace("@Model.PlaygroundSettings", Serializer.Serialize(_options.PlaygroundSettings));

                _playgroundCSHtml = builder.ToString();
            }

            return _playgroundCSHtml;
        }
    }
}
