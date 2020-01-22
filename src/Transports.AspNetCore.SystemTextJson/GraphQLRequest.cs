using System.Text.Json.Serialization;

namespace GraphQL.Server.Transports.AspNetCore.SystemTextJson
{
    public class GraphQLRequest : Server.Common.GraphQLRequest
    {
        [JsonPropertyName(QueryKey)]
        public override string Query { get; set; }

        [JsonPropertyName(VariablesKey)]
        public override Inputs Variables { get; set; }

        [JsonPropertyName(OperationNameKey)]
        public override string OperationName { get; set; }
    }
}
