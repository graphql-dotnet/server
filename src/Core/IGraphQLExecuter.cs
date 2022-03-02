using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Transport;
using GraphQL.Types;

namespace GraphQL.Server
{
    /// <summary>
    /// GraphQL query, mutation and subscription executer.
    /// </summary>
    public interface IGraphQLExecuter
    {
        /// <summary>
        /// Execute operation
        /// </summary>
        Task<ExecutionResult> ExecuteAsync(GraphQLRequest request, IDictionary<string, object> context, IServiceProvider requestServices, CancellationToken cancellationToken = default);
    }

    public interface IGraphQLExecuter<TSchema> : IGraphQLExecuter
        where TSchema : ISchema
    {
        TSchema Schema { get; }
    }
}
