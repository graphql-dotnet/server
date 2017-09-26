using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GraphQL.Transports.AspNetCore.Requests
{
    public class GraphQuery
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
            if (variables == null)
                return null;

            var inputs = variables.GetValue() as Dictionary<string, object>;

            return new Inputs(inputs);
        }
    }
}
