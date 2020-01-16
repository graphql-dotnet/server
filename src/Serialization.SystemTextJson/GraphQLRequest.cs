using GraphQL.Server.Common;
using System.Text.Json.Serialization;

namespace GraphQL.Server.Serialization.SystemTextJson
{
    public class GraphQLRequest : Common.GraphQLRequest
    {
        [JsonPropertyName(GraphQLRequestProperties.QueryKey)]
        public override string Query { get; set; }

        [JsonPropertyName(GraphQLRequestProperties.VariablesKey)]
        public override string Variables { get; set; }

        [JsonPropertyName(GraphQLRequestProperties.OperationNameKey)]
        public override string OperationName { get; set; }
    }
}
