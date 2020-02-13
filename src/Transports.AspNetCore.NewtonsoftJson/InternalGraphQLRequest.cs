using GraphQL.Server.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GraphQL.Server.Transports.AspNetCore.NewtonsoftJson
{
    /// <summary>
    /// A type for deserializing directly into that suits the System.Text.Json serializer.
    /// </summary>
    internal class InternalGraphQLRequest
    {
        [JsonProperty(GraphQLRequest.OperationNameKey)]
        public string OperationName { get; set; }

        [JsonProperty(GraphQLRequest.QueryKey)]
        public string Query { get; set; }

        [JsonProperty(GraphQLRequest.VariablesKey)]
        public JObject Variables { get; set; }
    }
}
