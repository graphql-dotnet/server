using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GraphQL.Server.Transports.AspNetCore.SystemTextJson
{
    public class GraphQLRequest : Server.Common.GraphQLRequest
    {
        [JsonPropertyName(QueryKey)]
        public override string Query { get; set; }

        [JsonPropertyName(OperationNameKey)]
        public override string OperationName { get; set; }

        /// <remarks>
        /// Population of this property during deserialization from JSON requires
        /// <see cref="GraphQL.SystemTextJson.ObjectDictionaryConverter"/>.
        /// </remarks>
        [JsonPropertyName(VariablesKey)]
        public Dictionary<string, object> Variables { get; set; }

        public override Inputs GetInputs() => Variables.ToInputs();
    }
}
