using System;

namespace Demo.Azure.Functions.GraphQL.Infrastructure
{
    internal class GraphQLBadRequestException : Exception
    {
        public GraphQLBadRequestException(string message)
            : base(message)
        { }
    }
}
