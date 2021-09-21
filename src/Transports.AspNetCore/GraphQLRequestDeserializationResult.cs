namespace GraphQL.Server.Transports.AspNetCore
{
    /// <summary>
    /// The result of a deserialization from a HTTP request body
    /// into a single GraphQLRequest or multiple (in the case of a batch request).
    /// </summary>
    public class GraphQLRequestDeserializationResult
    {
        /// <summary>
        /// A deserialized GraphQL request,
        /// populated if the HTTP request body contained a single JSON object.
        /// </summary>
        public GraphQLRequest Single { get; set; }

        /// <summary>
        /// A batch of deserialized GraphQL requests,
        /// populated if the HTTP request body contained an array of JSON objects.
        /// </summary>
        public GraphQLRequest[] Batch { get; set; }
    }
}
