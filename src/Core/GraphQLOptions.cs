using GraphQL.Server.Internal;
using GraphQL.Validation.Complexity;
using System;

namespace GraphQL.Server
{
    public class GraphQLOptions
    {
        public ComplexityConfiguration ComplexityConfiguration { get; set; }

        public bool EnableMetrics { get; set; } = true;

        public bool ExposeExceptions { get; set; }

        public bool SetFieldMiddleware { get; set; } = true;

        public Type GraphQLExecuterType { get; set; } = typeof(DefaultGraphQLExecuter<>);
    }
}
