using GraphQL.Validation.Complexity;

namespace GraphQL.Server
{
    public class GraphQLOptions
    {
        public ComplexityConfiguration ComplexityConfiguration { get; set; }

        public bool EnableMetrics { get; set; } = true;

        public bool ExposeExceptions { get; set; }

        public bool SetFieldMiddleware { get; set; } = true;
    }
}
