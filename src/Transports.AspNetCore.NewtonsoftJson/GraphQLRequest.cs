using Newtonsoft.Json;

namespace GraphQL.Server.Transports.AspNetCore.NewtonsoftJson
{
    public class GraphQLRequest : Server.Common.GraphQLRequest
    {
        [JsonProperty(QueryKey)]
        public override string Query { get; set; }

        [JsonProperty(VariablesKey)]
        public override Inputs Variables { get; set; }

        [JsonProperty(OperationNameKey)]
        public override string OperationName { get; set; }
    }
}
