using GraphQL.NewtonsoftJson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GraphQL.Server.Transports.AspNetCore.NewtonsoftJson
{
    public class GraphQLRequest : Server.Common.GraphQLRequest
    {
        [JsonProperty(OperationNameKey)]
        public override string OperationName { get; set; }

        [JsonProperty(QueryKey)]
        public override string Query { get; set; }

        [JsonProperty(VariablesKey)]
        public JObject Variables { get; set; }

        public override Inputs GetInputs() => Variables.ToInputs();
    }
}
