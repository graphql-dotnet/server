#nullable enable

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using GraphQL.Subscription;
using Microsoft.Extensions.Logging;

namespace GraphQL.Server.Transports.Subscriptions.Abstractions
{
    /// <summary>
    ///     Internal observer of the subscription
    /// </summary>
    public class Subscription : IObserver<ExecutionResult>, IDisposable
    {
        private Action<Subscription>? _completed;
        private readonly ILogger<Subscription> _logger;
        private IWriterPipeline? _writer;
        private IDisposable? _unsubscribe;

        public Subscription(string id,
            OperationMessagePayload payload,
            SubscriptionExecutionResult result,
            IWriterPipeline writer,
            Action<Subscription>? completed,
            ILogger<Subscription> logger)
        {
            _writer = writer;
            _completed = completed;
            _logger = logger;
            Id = id;
            OriginalPayload = payload;

            Subscribe(result);
        }

        public string Id { get; }

        public OperationMessagePayload OriginalPayload { get; }

        public void OnCompleted()
        {
            _logger.LogDebug("Subscription: {subscriptionId} completing", Id);
            _writer?.Post(new OperationMessage
            {
                Type = MessageType.GQL_COMPLETE,
                Id = Id
            });

            _completed?.Invoke(this);
            _unsubscribe?.Dispose();
            _completed = null;
            _writer = null;
            _unsubscribe = null;
        }

        public void OnError(Exception error) => throw new NotImplementedException();

        public void OnNext(ExecutionResult value)
        {
            _logger.LogDebug("Subscription: {subscriptionId} got data", Id);
            _writer?.Post(new OperationMessage
            {
                Type = MessageType.GQL_DATA,
                Id = Id,
                Payload = value
            });
        }

        public Task UnsubscribeAsync()
        {
            _logger.LogDebug("Subscription: {subscriptionId} unsubscribing", Id);
            _unsubscribe?.Dispose();
            var writer = _writer;
            _writer = null;
            _unsubscribe = null;
            _completed = null;
            return writer?.SendAsync(new OperationMessage
            {
                Type = MessageType.GQL_COMPLETE,
                Id = Id
            }) ?? Task.CompletedTask;
        }

        private void Subscribe(SubscriptionExecutionResult result)
        {
            var stream = result.Streams!.Values.Single();
            _unsubscribe = stream.Synchronize().Subscribe(this);
            _logger.LogDebug("Subscription: {subscriptionId} subscribed", Id);
        }

        public virtual void Dispose()
        {
            _unsubscribe?.Dispose();
            _unsubscribe = null;
            _writer = null;
            _completed = null;
        }
    }
}
