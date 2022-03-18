#nullable enable

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using GraphQL.Subscription;
using GraphQL.Transport;
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
            GraphQLRequest payload,
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

        public GraphQLRequest OriginalPayload { get; }

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

        /// <summary>
        /// Handles errors that are raised from the source.
        /// </summary>
        public void OnError(Exception error)
        {
            // pass along exceptions to the client
            if (error is ExecutionError executionError)
            {
                // exceptions should already be wrapped by the GraphQL engine
                OnNext(new ExecutionResult { Errors = new ExecutionErrors { executionError } });
            }
            else
            {
                // in the unlikely event that an unhandled exception delegate throws an exception,
                // or for any other reason it is not an ExecutionError instance, wrap the error
                OnNext(new ExecutionResult { Errors = new ExecutionErrors { new ExecutionError($"Unhandled error of type {error.GetType().Name}") } });
            }
            // Optionally we can disconnect the client by calling OnComplete() here
            //
            // https://docs.microsoft.com/en-us/dotnet/standard/events/observer-design-pattern-best-practices
            // > Once the provider calls the OnError or IObserver<T>.OnCompleted method, there should be
            // > no further notifications, and the provider can unsubscribe its observers.
        }

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
