using System;
using GraphQL.Conversion;
using GraphQL.Execution;
using GraphQL.Introspection;
using GraphQL.Validation.Complexity;

namespace GraphQL.Server
{
    /// <summary>
    /// Options to configure <see cref="DefaultGraphQLExecuter{TSchema}"/>.
    /// </summary>
    public class GraphQLOptions
    {
        public ComplexityConfiguration ComplexityConfiguration { get; set; }

        /// <summary>
        /// This setting essentially allows Apollo Tracing.
        /// </summary>
        public bool EnableMetrics { get; set; } = true;

        public INameConverter NameConverter { get; set; }

        public Action<UnhandledExceptionContext> UnhandledExceptionDelegate = ctx => { };

        public ISchemaFilter SchemaFilter { get; set; } = new DefaultSchemaFilter();

        /// <summary>If set, limits the maximum number of nodes executed in parallel</summary>
        public int? MaxParallelExecutionCount { get; set; }
    }
}
