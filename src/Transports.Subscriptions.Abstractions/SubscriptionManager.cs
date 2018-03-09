using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using GraphQL.Subscription;
using Newtonsoft.Json.Linq;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    public class SubscriptionManager : ISubscriptionManager
    {
        private readonly IGraphQLExecuter _executer;

        private readonly ConcurrentDictionary<string, Subscription> _subscriptions =
            new ConcurrentDictionary<string, Subscription>();

        public SubscriptionManager(IGraphQLExecuter executer)
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
            if (_subscriptions.TryRemove(id, out var removed))
                return removed.UnsubscribeAsync();

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
            var result = await _executer.ExecuteAsync(
                payload.OperationName,
                payload.Query,
                payload.Variables);

            if (result.Errors != null && result.Errors.Any())
            {
                await writer.SendAsync(new OperationMessage
                {
                    Type = MessageType.GQL_ERROR,
                    Id = id,
                    Payload = JObject.FromObject(result)
                });

                return null;
            }

            // is sub
            if (result is SubscriptionExecutionResult subscriptionExecutionResult)
            {
                if (subscriptionExecutionResult.Streams?.Values.SingleOrDefault() == null)
                {
                    await writer.SendAsync(new OperationMessage
                    {
                        Type = MessageType.GQL_ERROR,
                        Id = id,
                        Payload = JObject.FromObject(result)
                    });

                    return null;
                }

                return new Subscription(
                    id,
                    payload,
                    subscriptionExecutionResult,
                    writer,
                    sub => _subscriptions.TryRemove(id, out _));
            }

            //is query or mutation
            await writer.SendAsync(new OperationMessage
            {
                Type = MessageType.GQL_DATA,
                Id = id,
                Payload = JObject.FromObject(result)
            });

            await writer.SendAsync(new OperationMessage
            {
                Type = MessageType.GQL_COMPLETE,
                Id = id
            });

            return null;
        }
    }
}