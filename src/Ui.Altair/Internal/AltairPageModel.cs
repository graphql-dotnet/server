using System.IO;
using System.Text;

namespace GraphQL.Server.Ui.Altair.Internal
{
    // https://docs.microsoft.com/en-us/aspnet/core/mvc/razor-pages/?tabs=netcore-cli
    internal class AltairPageModel
    {
        private string _altairCSHtml;

        private readonly GraphQLAltairOptions _options;

        public AltairPageModel(GraphQLAltairOptions options)
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
                    .Replace("@Model.Headers", Serializer.Serialize(_options.Headers));

                _altairCSHtml = builder.ToString();
            }

            return _altairCSHtml;
        }
    }
}
