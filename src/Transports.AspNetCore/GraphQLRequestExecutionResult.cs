using System;

namespace GraphQL.Server.Transports.AspNetCore
{
    /// <summary>
    /// Represents the result of a GraphQL operation. Single GraphQL request may contain several operations, that is, be a batched request.
    /// </summary>
    public readonly struct GraphQLRequestExecutionResult
    {
        /// <summary>
        /// Creates <see cref="GraphQLRequestExecutionResult"/>.
        /// </summary>
        /// <param name="request">Executed GraphQL request.</param>
        /// <param name="result">Result of execution.</param>
        /// <param name="elapsed">Elapsed time.</param>
        /// <param name="indexInBatch">Index of the executed request (starting with 0) in case of a batched request, otherwise <see langword="null"/>.</param>
        public GraphQLRequestExecutionResult(GraphQLRequest request, ExecutionResult result, TimeSpan elapsed, int? indexInBatch = null)
        {
            Request = request;
            Result = result;
            Elapsed = elapsed;
            IndexInBatch = indexInBatch;
        }

        /// <summary>
        /// Executed GraphQL request.
        /// </summary>
        public GraphQLRequest Request { get; }

        /// <summary>
        /// Result of execution.
        /// </summary>
        public ExecutionResult Result { get; }

        /// <summary>
        /// Elapsed time.
        /// </summary>
        public TimeSpan Elapsed { get; }

        /// <summary>
        /// Index of the executed request (starting with 0) in case of a batched request, otherwise <see langword="null"/>.
        /// </summary>
        public int? IndexInBatch { get; }
    }
}
