using GraphQL.Server.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GraphQL.Server.Transports.AspNetCore.NewtonsoftJson
{
    /// <summary>
    /// A type for deserializing directly into that suits the NewtonsoftJson serializer.
    /// </summary>
    internal sealed class InternalGraphQLRequest
    {
        [JsonProperty(GraphQLRequest.OPERATION_NAME_KEY)]
        public string OperationName { get; set; }

        [JsonProperty(GraphQLRequest.QUERY_KEY)]
        public string Query { get; set; }

        [JsonProperty(GraphQLRequest.VARIABLES_KEY)]
        public JObject Variables { get; set; }

        [JsonProperty(GraphQLRequest.EXTENSIONS_KEY)]
        public JObject Extensions { get; set; }
    }
}
