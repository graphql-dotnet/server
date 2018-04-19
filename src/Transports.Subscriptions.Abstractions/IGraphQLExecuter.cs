using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    /// <summary>
    ///     GraphQL query,mutation and subscription executer
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
        /// <returns></returns>
        Task<ExecutionResult> ExecuteAsync(string operationName, string query, JObject variables,
            MessageHandlingContext context);
    }
}