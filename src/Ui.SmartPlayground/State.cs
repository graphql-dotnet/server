using System.Text.Json.Serialization;

namespace GraphQL.Server.Ui.SmartPlayground
{
    internal class State
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }
    }
}