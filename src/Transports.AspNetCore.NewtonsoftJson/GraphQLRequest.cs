using GraphQL.NewtonsoftJson;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace GraphQL.Server.Transports.AspNetCore.NewtonsoftJson
{
    public class GraphQLRequest : Server.Common.GraphQLRequest
    {
        [JsonProperty(OperationNameKey)]
        public override string OperationName { get; set; }

        [JsonProperty(QueryKey)]
        public override string Query { get; set; }

        [JsonProperty(VariablesKey)]
        //[JsonConverter()] // TODO: Similar to SystemTextJson, implement a converter for this guy
        public override Dictionary<string, object> Variables { get; set; }

        public override Inputs GetInputs() => Variables.ToInputs();
    }
}
