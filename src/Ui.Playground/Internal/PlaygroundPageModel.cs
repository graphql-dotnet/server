using System.IO;
using System.Text;
using System.Text.Json;

namespace GraphQL.Server.Ui.Playground.Internal
{
    // https://docs.microsoft.com/en-us/aspnet/core/mvc/razor-pages/?tabs=netcore-cli
    internal sealed class PlaygroundPageModel
    {
        private string _playgroundCSHtml;

        private readonly PlaygroundOptions _options;

        public PlaygroundPageModel(PlaygroundOptions options)
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
                    .Replace("@Model.SubscriptionsEndPoint", _options.SubscriptionsEndPoint)
                    .Replace("@Model.GraphQLConfig", JsonSerializer.Serialize<object>(_options.GraphQLConfig))
                    .Replace("@Model.Headers", JsonSerializer.Serialize<object>(_options.Headers))
                    .Replace("@Model.PlaygroundSettings", JsonSerializer.Serialize<object>(_options.PlaygroundSettings));

                _playgroundCSHtml = builder.ToString();
            }

            return _playgroundCSHtml;
        }
    }
}
