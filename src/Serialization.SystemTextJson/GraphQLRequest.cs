using System.Text.Json.Serialization;

namespace GraphQL.Server.Serialization.SystemTextJson
{
    public class GraphQLRequest : Common.GraphQLRequest
    {
        [JsonPropertyName(QueryKey)]
        public override string Query { get; set; }

        [JsonPropertyName(VariablesKey)]
        public override string Variables { get; set; }

        [JsonPropertyName(OperationNameKey)]
        public override string OperationName { get; set; }
    }
}
