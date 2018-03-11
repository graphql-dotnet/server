using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    public interface ISubscriptionManager : IEnumerable<Subscription>
    {
        Task SubscribeAsync(string id, OperationMessagePayload payload, IWriterPipeline writer);

        Task UnsubscribeAsync(string id);
    }
}