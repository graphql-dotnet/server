using GraphQL.Server.Transports.AspNetCore.Common;
using Newtonsoft.Json;

namespace GraphQL.Server.Serialization.NewtonsoftJson
{
    public class GraphQLRequest : Transports.AspNetCore.Common.GraphQLRequest
    {
        [JsonProperty(GraphQLRequestProperties.QueryKey)]
        public override string Query { get; set; }

        [JsonProperty(GraphQLRequestProperties.VariablesKey)]
        public override string Variables { get; set; }

        [JsonProperty(GraphQLRequestProperties.OperationNameKey)]
        public override string OperationName { get; set; }
    }
}
