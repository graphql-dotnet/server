using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    public interface IGraphQLExecuter
    {
        Task<ExecutionResult> ExecuteAsync(string operationName, string query, JObject variables);
    }
}