using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using GraphQL.Subscription;
using Newtonsoft.Json.Linq;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    public class Subscription : IObserver<ExecutionResult>
    {
        private readonly Action<Subscription> _completed;
        private readonly ITargetBlock<OperationMessage> _writer;
        private IDisposable _unsubscribe;

        public Subscription(string id,
            OperationMessagePayload payload,
            SubscriptionExecutionResult result,
            ITargetBlock<OperationMessage> writer,
            Action<Subscription> completed)
        {
            _writer = writer;
            _completed = completed;
            Id = id;
            OriginalPayload = payload;

            Subscribe(result);
        }

        public string Id { get; }

        public OperationMessagePayload OriginalPayload { get; }

        public void OnCompleted()
        {
            _writer.Post(new OperationMessage
            {
                Type = MessageType.GQL_COMPLETE,
                Id = Id
            });

            _completed?.Invoke(this);
            _unsubscribe.Dispose();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(ExecutionResult value)
        {
            _writer.Post(new OperationMessage
            {
                Type = MessageType.GQL_DATA,
                Id = Id,
                Payload = JObject.FromObject(value)
            });
        }

        private void Subscribe(SubscriptionExecutionResult result)
        {
            var stream = result.Streams.Values.Single();
            _unsubscribe = stream.Subscribe(this);
        }

        public Task UnsubscribeAsync()
        {
            _unsubscribe.Dispose();
            return _writer.SendAsync(new OperationMessage
            {
                Type = MessageType.GQL_COMPLETE,
                Id = Id
            });
        }
    }
}