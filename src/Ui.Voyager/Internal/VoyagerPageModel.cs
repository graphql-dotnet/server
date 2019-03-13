using System.IO;
using System.Reflection;
using System.Text;

namespace GraphQL.Server.Ui.Voyager.Internal
{
    // https://docs.microsoft.com/en-us/aspnet/core/mvc/razor-pages/?tabs=netcore-cli
    internal class VoyagerPageModel
    {
        private string _voyagerCSHtml;

        private readonly GraphQLVoyagerOptions _settings;

        public VoyagerPageModel(GraphQLVoyagerOptions settings) => _settings = settings;

        public string Render()
        {
            if (_voyagerCSHtml == null)
            {
                var assembly = typeof(VoyagerPageModel).GetTypeInfo().Assembly;

                using (var manifestResourceStream = assembly.GetManifestResourceStream("GraphQL.Server.Ui.Voyager.Internal.voyager.cshtml"))
                {
                    using (var streamReader = new StreamReader(manifestResourceStream))
                    {
                        var builder = new StringBuilder(streamReader.ReadToEnd());

                        builder.Replace("@Model.GraphQLEndPoint", _settings.GraphQLEndPoint);

                        _voyagerCSHtml = builder.ToString();
                    }
                }
            }

            return _voyagerCSHtml;
        }
    }
}
