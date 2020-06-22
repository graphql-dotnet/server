using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GraphQL.Server.Transports.AspNetCore.SystemTextJson
{
    /// <summary>
    /// A type for deserializing directly into that suits the System.Text.Json serializer.
    /// </summary>
    internal sealed class InternalGraphQLRequest
    {
        [JsonPropertyName(GraphQLRequest.QUERY_KEY)]
        public string Query { get; set; }

        [JsonPropertyName(GraphQLRequest.OPERATION_NAME_KEY)]
        public string OperationName { get; set; }

        /// <remarks>
        /// Population of this property during deserialization from JSON requires
        /// <see cref="GraphQL.SystemTextJson.ObjectDictionaryConverter"/>.
        /// </remarks>
        [JsonPropertyName(GraphQLRequest.VARIABLES_KEY)]
        public Dictionary<string, object> Variables { get; set; }
    }
}
