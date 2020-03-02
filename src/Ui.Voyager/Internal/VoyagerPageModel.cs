using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GraphQL.Server.Ui.Voyager.Internal
{
    // https://docs.microsoft.com/en-us/aspnet/core/mvc/razor-pages/?tabs=netcore-cli
    internal class VoyagerPageModel
    {
        private string _voyagerCSHtml;

        private readonly GraphQLVoyagerOptions _options;

        public VoyagerPageModel(GraphQLVoyagerOptions options)
        {
            _options = options;
        }

        public string Render()
        {
            if (_voyagerCSHtml == null)
            {
                using var manifestResourceStream = typeof(VoyagerPageModel).Assembly.GetManifestResourceStream("GraphQL.Server.Ui.Voyager.Internal.voyager.cshtml");
                using var streamReader = new StreamReader(manifestResourceStream);

                var headers = new Dictionary<string, object>
                {
                    ["Accept"] = "application/json",
                    ["Content-Type"] = "application/json",
                };

                if ( _options.Headers?.Count > 0)
                {
                    foreach (var item in _options.Headers)
                        headers[item.Key] = item.Value;
                }

                var builder = new StringBuilder(streamReader.ReadToEnd())
                    .Replace("@Model.GraphQLEndPoint", _options.GraphQLEndPoint)
                    .Replace("@Model.Headers", Serializer.Serialize(headers));

                _voyagerCSHtml = builder.ToString();
            }

            return _voyagerCSHtml;
        }
    }
}
