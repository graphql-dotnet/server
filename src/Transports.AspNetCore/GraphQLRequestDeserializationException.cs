using System;

namespace GraphQL.Server.Transports.AspNetCore
{
    public class GraphQLRequestDeserializationException : Exception
    {
        public GraphQLRequestDeserializationException(string message) : base(message)
        {
        }

        public GraphQLRequestDeserializationException(Exception inner) : base(inner.Message, inner)
        {
        }

        public static GraphQLRequestDeserializationException InvalidFirstChar()
        {
            return new GraphQLRequestDeserializationException("Body text should start with '{' for normal graphql query or with '[' for batched query.");
        }
    }
}
