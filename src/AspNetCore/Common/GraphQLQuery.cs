using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GraphQL.Server.Transports.AspNetCore.Common
{
    public class GraphQLQuery
    {
        [JsonProperty("query")]
        public string Query { get; set; }

        [JsonProperty("variables")]
        public JObject Variables { get; set; }

        [JsonProperty("operationName")]
        public string OperationName { get; set; }

        public Inputs GetInputs()
        {
            return GetInputs(Variables);
        }

        public static Inputs GetInputs(JObject variables)
        {
            return variables.ToInputs();
        }
    }
}
