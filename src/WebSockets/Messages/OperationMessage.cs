using Newtonsoft.Json;

namespace GraphQL.Server.Transports.WebSockets.Messages
{
    public class OperationMessage
    {
        [JsonProperty("type")] public string Type { get; set; }

        [JsonProperty("id")] public string Id { get; set; }

        [JsonProperty("payload")] public dynamic Payload { get; set; }
    }
}
