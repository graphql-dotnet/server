using System;
using GraphQL.Execution;
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

        public Action<UnhandledExceptionContext> UnhandledExceptionDelegate = ctx => { };

        /// <summary>If set, limits the maximum number of nodes executed in parallel</summary>
        public int? MaxParallelExecutionCount { get; set; }
    }
}
