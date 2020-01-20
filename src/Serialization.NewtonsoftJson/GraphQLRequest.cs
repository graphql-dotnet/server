using Newtonsoft.Json;

namespace GraphQL.Server.Serialization.NewtonsoftJson
{
    public class GraphQLRequest : Common.GraphQLRequest
    {
        [JsonProperty(QueryKey)]
        public override string Query { get; set; }

        [JsonProperty(VariablesKey)]
        public override Inputs Variables { get; set; }

        [JsonProperty(OperationNameKey)]
        public override string OperationName { get; set; }
    }
}
