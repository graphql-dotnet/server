using System.Text;
#if NET7_0_OR_GREATER
using System.Text.Json.Serialization;
#endif

namespace GraphQL.Server.Ui.Voyager.Internal;

// https://docs.microsoft.com/en-us/aspnet/core/mvc/razor-pages/?tabs=netcore-cli
internal sealed class VoyagerPageModel
{
    private string? _voyagerCSHtml;

    private readonly VoyagerOptions _options;

    public VoyagerPageModel(VoyagerOptions options)
    {
        _options = options;
    }

    public string Render()
    {
        if (_voyagerCSHtml == null)
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

            var requestCredentials = _options.RequestCredentials switch
            {
                RequestCredentials.Include => "include",
                RequestCredentials.SameOrigin => "same-origin",
                RequestCredentials.Omit => "omit",
                _ => throw new InvalidOperationException("The RequestCredentials property is invalid."),
            };

            var builder = new StringBuilder(streamReader.ReadToEnd())
                .Replace("@Model.GraphQLEndPoint", StringEncode(_options.GraphQLEndPoint))
                .Replace("@Model.Headers", JsonSerialize(headers))
                .Replace("@Model.RequestCredentials", requestCredentials);

            _voyagerCSHtml = _options.PostConfigure(_options, builder.ToString());
        }

        return _voyagerCSHtml;
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
