using System.Threading.Tasks;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    public interface IGraphQLExecuter
    {
        Task<ExecutionResult> ExecuteAsync(string operationName, string query, dynamic variables);
    }
}