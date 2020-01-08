using GraphQL.Server.Transports.AspNetCore.Common;
using System.Text.Json.Serialization;

namespace GraphQL.Server.Serialization.SystemTextJson
{
    public class GraphQLRequest : IGraphQLRequest
    {
        [JsonPropertyName(GraphQLRequestProperties.QueryKey)]
        public string Query { get; set; }

        [JsonPropertyName(GraphQLRequestProperties.VariablesKey)]
        public string Variables { get; set; }

        [JsonPropertyName(GraphQLRequestProperties.OperationNameKey)]
        public string OperationName { get; set; }
    }
}
