using System;

namespace GraphQL.Server.Transports.AspNetCore.Common
{
    public readonly struct GraphQLRequestExecutionResult
    {
        public GraphQLRequestExecutionResult(GraphQLRequest request, ExecutionResult result, TimeSpan elapsed, int? indexInBatch = null)
        {
            Request = request;
            Result = result;
            Elapsed = elapsed;
            IndexInBatch = indexInBatch;
        }

        public GraphQLRequest Request { get; }

        public ExecutionResult Result { get; }

        public TimeSpan Elapsed { get; }

        public int? IndexInBatch { get; }
    }
}
