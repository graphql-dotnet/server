using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace GraphQL.Server.Ui.Voyager.Internal
{
    // https://docs.microsoft.com/en-us/aspnet/core/mvc/razor-pages/?tabs=netcore-cli
    internal sealed class VoyagerPageModel
    {
        private string _voyagerCSHtml;

        private readonly VoyagerOptions _options;

        public VoyagerPageModel(VoyagerOptions options)
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

                if (_options.Headers?.Count > 0)
                {
                    foreach (var item in _options.Headers)
                        headers[item.Key] = item.Value;
                }

                var builder = new StringBuilder(streamReader.ReadToEnd())
                    .Replace("@Model.GraphQLEndPoint", _options.GraphQLEndPoint)
                    .Replace("@Model.Headers", JsonSerializer.Serialize<object>(headers));

                _voyagerCSHtml = builder.ToString();
            }

            return _voyagerCSHtml;
        }
    }
}
