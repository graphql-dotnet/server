using System.Text;

namespace GraphQL.Server.Ui.Altair.Internal;

// https://docs.microsoft.com/en-us/aspnet/core/mvc/razor-pages/?tabs=netcore-cli
internal sealed class AltairPageModel
{
    private string? _altairCSHtml;

    private readonly AltairOptions _options;

    public AltairPageModel(AltairOptions options)
    {
        _options = options;
    }

    public string Render()
    {
        if (_altairCSHtml == null)
        {
            using var manifestResourceStream = _options.IndexStream(_options);
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
                .Replace("@Model.SubscriptionsEndPoint", _options.SubscriptionsEndPoint)
                .Replace("@Model.Headers", JsonSerialize(headers));

            _altairCSHtml = _options.PostConfigure(_options, builder.ToString());
        }

        return _altairCSHtml;
    }

    private static string JsonSerialize(object value)
    {
#if NETSTANDARD2_0
        return Newtonsoft.Json.JsonConvert.SerializeObject(value);
#else
        return System.Text.Json.JsonSerializer.Serialize(value);
#endif
    }
}
