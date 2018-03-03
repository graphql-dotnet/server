using System.Threading.Tasks;
using GraphQL.Subscription;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    public interface ISubscriptionExecuter
    {
        Task<SubscriptionExecutionResult> SubscribeAsync(string operationName, string query, dynamic variables);
    }
}