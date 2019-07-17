using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Types;

namespace GraphQL.Server.Internal
{
    /// <summary>
    ///     GraphQL query, mutation and subscription executer
    /// </summary>
    public interface IGraphQLExecuter
    {
        /// <summary>
        ///     Execute operation
        /// </summary>
        /// <param name="operationName"></param>
        /// <param name="query"></param>
        /// <param name="variables"></param>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ExecutionResult> ExecuteAsync(string operationName, string query, Inputs variables, IDictionary<string, object> context, CancellationToken cancellationToken = default);
    }

    public interface IGraphQLExecuter<TSchema> : IGraphQLExecuter
        where TSchema : ISchema
    {
        TSchema Schema { get; }
    }
}