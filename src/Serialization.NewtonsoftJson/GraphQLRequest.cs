using GraphQL.Server.Transports.AspNetCore.Common;
using Newtonsoft.Json;

namespace GraphQL.Server.Serialization.NewtonsoftJson
{
    public class GraphQLRequest : IGraphQLRequest
    {
        [JsonProperty(GraphQLRequestProperties.QueryKey)]
        public string Query { get; set; }

        [JsonProperty(GraphQLRequestProperties.VariablesKey)]
        public string Variables { get; set; }

        [JsonProperty(GraphQLRequestProperties.OperationNameKey)]
        public string OperationName { get; set; }
    }
}
