using Newtonsoft.Json.Linq;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    public class OperationMessagePayload
    {
        public string Query { get; set; }

        public JObject Variables { get; set; }

        public string OperationName { get; set; }
    }
}