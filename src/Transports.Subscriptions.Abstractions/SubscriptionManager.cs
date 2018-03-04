using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using GraphQL.Subscription;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    public class SubscriptionManager : ISubscriptionManager
    {
        private readonly ISubscriptionExecuter _executer;

        private readonly ConcurrentDictionary<string, Subscription> _subscriptions =
            new ConcurrentDictionary<string, Subscription>();

        public SubscriptionManager(ISubscriptionExecuter executer)
        {
            _executer = executer;
        }

        public Subscription this[string id] => _subscriptions[id];


        public IEnumerator<Subscription> GetEnumerator()
        {
            return _subscriptions.Values.GetEnumerator();
        }

        public async Task SubscribeAsync(string id, OperationMessagePayload payload,
            ITargetBlock<OperationMessage> writer)
        {
            var subscription = await ExecuteAsync(id, payload, writer);

            if (subscription == null)
                return;

            _subscriptions[id] = subscription;
        }

        public Task UnsubscribeAsync(string id)
        {
            if (_subscriptions.TryRemove(id, out var removed)) return removed.UnsubscribeAsync();

            return Task.CompletedTask;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _subscriptions.Values.GetEnumerator();
        }

        private async Task<Subscription> ExecuteAsync(
            string id,
            OperationMessagePayload payload,
            ITargetBlock<OperationMessage> writer)
        {
            SubscriptionExecutionResult result = await _executer.SubscribeAsync(
                payload.OperationName,
                payload.Query,
                payload.Variables);

            if (result.Errors != null && result.Errors.Any())
            {
                await writer.SendAsync(new OperationMessage
                {
                    Type = MessageTypeConstants.GQL_ERROR,
                    Id = id,
                    Payload = result
                });

                return null;
            }

            if (result.Streams?.Values.SingleOrDefault() == null)
            {
                await writer.SendAsync(new OperationMessage
                {
                    Type = MessageTypeConstants.GQL_ERROR,
                    Id = id,
                    Payload = result
                });

                return null;
            }

            return new Subscription(
                id, 
                payload, 
                result, 
                writer, 
                completed: sub => _subscriptions.TryRemove(id, out var _));
        }
    }
}