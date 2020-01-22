using GraphQL.SystemTextJson;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GraphQL.Server.Transports.AspNetCore.SystemTextJson
{
    public class GraphQLRequest : Server.Common.GraphQLRequest
    {
        [JsonPropertyName(QueryKey)]
        public override string Query { get; set; }

        [JsonPropertyName(OperationNameKey)]
        public override string OperationName { get; set; }

        [JsonPropertyName(VariablesKey)]
        public JsonElement Variables { get; set; }

        public override Inputs GetInputs() => Variables.ToInputs();
    }
}
