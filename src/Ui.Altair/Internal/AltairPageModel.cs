using System.IO;
using System.Text;
using System.Text.Json;

namespace GraphQL.Server.Ui.Altair.Internal
{
    // https://docs.microsoft.com/en-us/aspnet/core/mvc/razor-pages/?tabs=netcore-cli
    internal sealed class AltairPageModel
    {
        private string _altairCSHtml;

        private readonly AltairOptions _options;

        public AltairPageModel(AltairOptions options)
        {
            _options = options;
        }

        public string Render()
        {
            if (_altairCSHtml == null)
            {
                using var manifestResourceStream = typeof(AltairPageModel).Assembly.GetManifestResourceStream("GraphQL.Server.Ui.Altair.Internal.altair.cshtml");
                using var streamReader = new StreamReader(manifestResourceStream);

                var builder = new StringBuilder(streamReader.ReadToEnd())
                    .Replace("@Model.GraphQLEndPoint", _options.GraphQLEndPoint)
                    .Replace("@Model.SubscriptionsEndPoint", _options.SubscriptionsEndPoint)
                    .Replace("@Model.Headers", JsonSerializer.Serialize<object>(_options.Headers));

                _altairCSHtml = builder.ToString();
            }

            return _altairCSHtml;
        }
    }
}
