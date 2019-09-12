using GraphQL.Conversion;
using GraphQL.Validation.Complexity;
using System;

namespace GraphQL.Server
{
    public class GraphQLOptions
    {
        public ComplexityConfiguration ComplexityConfiguration { get; set; }

        /// <summary>
        /// This setting essentially allows Apollo Tracing.
        /// </summary>
        public bool EnableMetrics { get; set; } = true;

        public bool ExposeExceptions { get; set; }

        public IFieldNameConverter FieldNameConverter { get; set; }

        public Func<Execution.ExecutionContext, Exception, Exception> UnhandledExceptionDelegate = (ctx, ex) => ex;
    }
}
