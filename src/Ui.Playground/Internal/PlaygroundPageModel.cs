using System.Text;
#if NET7_0_OR_GREATER
using System.Text.Json.Serialization;
#endif

namespace GraphQL.Server.Ui.Playground.Internal;

// https://docs.microsoft.com/en-us/aspnet/core/mvc/razor-pages/?tabs=netcore-cli
internal sealed class PlaygroundPageModel
{
    private string? _playgroundCSHtml;

    private readonly PlaygroundOptions _options;

    public PlaygroundPageModel(PlaygroundOptions options)
    {
        _options = options;
    }

    public string Render()
    {
        if (_playgroundCSHtml == null)
        {
            using var manifestResourceStream = _options.IndexStream(_options);
            using var streamReader = new StreamReader(manifestResourceStream);

            var headers = new Dictionary<string, object>
            {
                ["Accept"] = "application/json",
                // TODO: investigate, fails in Chrome
                // {
                //   "error": "Response not successful: Received status code 400"
                // }
                //
                // MediaTypeHeaderValue.TryParse(httpRequest.ContentType, out var mediaTypeHeader) from GraphQLHttpMiddleware
                // returns false because of
                // content-type: application/json, application/json

                //["Content-Type"] = "application/json",
            };

            if (_options.Headers?.Count > 0)
            {
                foreach (var item in _options.Headers)
                    headers[item.Key] = item.Value;
            }

            var builder = new StringBuilder(streamReader.ReadToEnd())
                .Replace("@Model.GraphQLEndPoint", StringEncode(_options.GraphQLEndPoint))
                .Replace("@Model.SubscriptionsEndPoint", StringEncode(_options.SubscriptionsEndPoint))
                .Replace("@Model.GraphQLConfig", JsonSerialize(_options.GraphQLConfig!))
                .Replace("@Model.Headers", JsonSerialize(headers))
                .Replace("@Model.PlaygroundSettings", JsonSerialize(_options.PlaygroundSettings));

            // Here, fully-qualified, absolute and relative URLs are supported for both the
            // GraphQLEndPoint and SubscriptionsEndPoint.  Those paths can be passed unmodified
            // to 'fetch', but for websocket connectivity, fully-qualified URLs are required.
            // So within the javascript, we convert the absolute/relative URLs to fully-qualified URLs.

            _playgroundCSHtml = _options.PostConfigure(_options, builder.ToString());
        }

        return _playgroundCSHtml;
    }

    // https://html.spec.whatwg.org/multipage/scripting.html#restrictions-for-contents-of-script-elements
    private static string StringEncode(string value) => value
        .Replace("\\", "\\\\")  // encode  \  as  \\
        .Replace("<", "\\x3C")  // encode  <  as  \x3C   -- so "<!--", "<script" and "</script" are handled correctly
        .Replace("'", "\\'")    // encode  '  as  \'
        .Replace("\"", "\\\""); // encode  "  as  \"

    private static string JsonSerialize(Dictionary<string, object> value)
    {
#if NETSTANDARD2_0
        return Newtonsoft.Json.JsonConvert.SerializeObject(value);
#elif NET7_0_OR_GREATER
        return System.Text.Json.JsonSerializer.Serialize(value, SourceGenerationContext.Default.DictionaryStringObject);
#else
        return System.Text.Json.JsonSerializer.Serialize(value);
#endif
    }
}

#if NET7_0_OR_GREATER
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(string))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}
#endif
