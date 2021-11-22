using System;

namespace GraphQL.Server.Transports.AspNetCore
{
    /// <summary>
    /// Exception used by <see cref="IGraphQLRequestDeserializer"/> implementations
    /// when deserialization failed.
    /// </summary>
    public class GraphQLRequestDeserializationException : Exception
    {
        public GraphQLRequestDeserializationException(string message) : base(message)
        {
        }

        public GraphQLRequestDeserializationException(Exception inner) : base(inner.Message, inner)
        {
        }

        public GraphQLRequestDeserializationException(string message, Exception inner) : base(message, inner)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="GraphQLRequestDeserializationException"/> for a situations
        /// when the first symbol of JSON body neither '{' nor '['.
        /// </summary>
        public static GraphQLRequestDeserializationException InvalidFirstChar()
        {
            return new GraphQLRequestDeserializationException("Body text should start with '{' for normal graphql query or with '[' for batched query.");
        }
    }
}
