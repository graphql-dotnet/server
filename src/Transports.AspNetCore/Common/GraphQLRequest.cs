using Newtonsoft.Json.Linq;

#if NETSTANDARD2_0
using Newtonsoft.Json;
#else
using System.Text.Json.Serialization;
#endif

namespace GraphQL.Server.Transports.AspNetCore.Common
{
    public class GraphQLRequest
    {
        public const string QueryKey = "query";
        public const string VariablesKey = "variables";
        public const string OperationNameKey = "operationName";

#if NETSTANDARD2_0
        [JsonProperty(QueryKey)]
#else
        [JsonPropertyName(QueryKey)]
#endif
        public string Query { get; set; }

#if NETSTANDARD2_0
        [JsonProperty(VariablesKey)]
#else
        [JsonPropertyName(VariablesKey)]
#endif
        public JObject Variables { get; set; }

#if NETSTANDARD2_0
        [JsonProperty(OperationNameKey)]
#else
        [JsonPropertyName(OperationNameKey)]
#endif
        public string OperationName { get; set; }

        public Inputs GetInputs() => GetInputs(Variables);

        public static Inputs GetInputs(JObject variables) => variables?.ToInputs();
    }
}
