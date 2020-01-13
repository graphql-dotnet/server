using GraphQL.Server.Transports.AspNetCore.Common;
using System.Text.Json.Serialization;

namespace GraphQL.Server.Serialization.SystemTextJson
{
    public class GraphQLRequest : Transports.AspNetCore.Common.GraphQLRequest
    {
        [JsonPropertyName(GraphQLRequestProperties.QueryKey)]
        public override string Query { get; set; }

        [JsonPropertyName(GraphQLRequestProperties.VariablesKey)]
        public override string Variables { get; set; }

        [JsonPropertyName(GraphQLRequestProperties.OperationNameKey)]
        public override string OperationName { get; set; }
    }
}
