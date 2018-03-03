using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using GraphQL.Subscription;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    public class Subscription
    {
        private readonly ITargetBlock<OperationMessage> _writer;

        public Subscription(string id, OperationMessagePayload payload, SubscriptionExecutionResult result,
            ITargetBlock<OperationMessage> writer)
        {
            _writer = writer;
            Id = id;
            OriginalPayload = payload;
        }

        public string Id { get; }

        public OperationMessagePayload OriginalPayload { get; }

        public Task UnsubscribeAsync()
        {
            return _writer.SendAsync(new OperationMessage
            {
                Type = MessageTypeConstants.GQL_COMPLETE,
                Id = Id
            });
        }
    }
}