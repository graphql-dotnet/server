#nullable enable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Subscription;
using GraphQL.Transport;

namespace GraphQL.Server.Transports.Subscriptions.WebSockets.Shane
{
    public abstract class OperationMessageServer : IOperationMessageReceiveStream
    {
        private volatile int _initialized = 0;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly TimeSpan _keepAliveTimeout;
        private readonly TimeSpan _connectionInitWaitTimeout;

        /// <summary>
        /// Returns a <see cref="IOperationMessageSendStream"/> instance that can be used
        /// to send messages to the client.
        /// </summary>
        protected IOperationMessageSendStream Client { get; }

        /// <summary>
        /// Returns a <see cref="System.Threading.CancellationToken"/> that is signaled
        /// when the WebSockets connection is closed.
        /// </summary>
        protected CancellationToken CancellationToken { get; }

        /// <summary>
        /// Returns a synchronized list of subscriptions.
        /// </summary>
        protected SubscriptionList Subscriptions { get; }

        /// <summary>
        /// Initailizes a new instance with the specified parameters.
        /// </summary>
        /// <param name="sendStream">The WebSockets stream used to send data packets or close the connection.</param>
        /// <param name="connectionInitWaitTimeout">The amount of time to wait for a connection initialization message before terminating the connection. <see cref="Timeout.InfiniteTimeSpan"/> can be used to disable the timeout.</param>
        /// <param name="keepAliveTimeout">The periodic interval to send keep-alive messages receiving a connection initialization message. <see cref="Timeout.InfiniteTimeSpan"/> can be used to disable the keep-alive signal.</param>
        public OperationMessageServer(
            IOperationMessageSendStream sendStream,
            TimeSpan connectionInitWaitTimeout,
            TimeSpan keepAliveTimeout)
        {
            if (connectionInitWaitTimeout != Timeout.InfiniteTimeSpan && connectionInitWaitTimeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(connectionInitWaitTimeout));
            if (keepAliveTimeout != Timeout.InfiniteTimeSpan && keepAliveTimeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(keepAliveTimeout));
            Client = sendStream ?? throw new ArgumentNullException(nameof(sendStream));
            _cancellationTokenSource = new();
            CancellationToken = _cancellationTokenSource.Token;
            Subscriptions = new(CancellationToken);
            _keepAliveTimeout = keepAliveTimeout;
            _connectionInitWaitTimeout = connectionInitWaitTimeout;
        }

        /// <inheritdoc/>
        public void StartConnectionInitTimer()
        {
            if (_connectionInitWaitTimeout != Timeout.InfiniteTimeSpan)
            {
                Task.Run(async () =>
                {
                    await Task.Delay(_connectionInitWaitTimeout, CancellationToken);
                    if (_initialized == 0)
                        await OnConnectionInitWaitTimeout();
                });
            }
        }

        /// <summary>
        /// Executes once the initialization timeout has expired without being initialized.
        /// </summary>
        protected virtual Task OnConnectionInitWaitTimeout()
            => ErrorConnectionInitializationTimeout();

        /// <summary>
        /// Called when the WebSocket connection (not necessarily the HTTP connection) has been terminated.
        /// Disposes of all active subscriptions, cancels all existing requests,
        /// and prevents any further responses.
        /// </summary>
        public virtual void Dispose()
        {
            var cts = Interlocked.Exchange(ref _cancellationTokenSource, null);
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
                Subscriptions.Dispose(); //redundant
            }
        }

        /// <summary>
        /// Indicates if the connection has been initialized yet.
        /// </summary>
        protected bool Initialized
            => _initialized == 1;

        /// <summary>
        /// Sets the initialized flag if it has not already been set.
        /// Returns <see langword="false"/> if it was already set.
        /// </summary>
        protected bool TryInitialize()
            => Interlocked.Exchange(ref _initialized, 1) == 0;

        /// <summary>
        /// Executes when a message has been received from the client.
        /// </summary>
        /// <exception cref="OperationCanceledException"/>
        public abstract Task OnMessageReceivedAsync(OperationMessage message);

        /// <summary>
        /// Executes upon a request to close the connection from the client.
        /// </summary>
        protected virtual Task OnCloseConnection()
            => Client.CloseConnectionAsync();

        /// <summary>
        /// Sends a fatal error message indicating that the initialization timeout has expired
        /// without the connection being initialized.
        /// </summary>
        protected virtual Task ErrorConnectionInitializationTimeout()
            => Client.CloseConnectionAsync(4408, "Connection initialization timeout");

        /// <summary>
        /// Sends a fatal error message indicating that the client attempted to initialize
        /// the connection more than one time.
        /// </summary>
        protected virtual Task ErrorTooManyInitializationRequests()
            => Client.CloseConnectionAsync(4429, "Too many initialization requests");

        /// <summary>
        /// Sends a fatal error message indicating that the client attempted to subscribe
        /// to an event stream before initialization was complete.
        /// </summary>
        protected virtual Task ErrorNotInitialized()
            => Client.CloseConnectionAsync(4401, "Unauthorized");

        /// <summary>
        /// Sends a fatal error message indicating that the client attempted to use an
        /// unrecognized message type.
        /// </summary>
        protected virtual Task ErrorUnrecognizedMessage()
            => Client.CloseConnectionAsync(4400, "Unrecognized message");

        /// <summary>
        /// Sends a fatal error message indicating that the client attempted to subscribe
        /// to an event stream with an empty id.
        /// </summary>
        protected virtual Task ErrorIdCannotBeBlank()
            => Client.CloseConnectionAsync(4400, "Id cannot be blank");

        /// <summary>
        /// Sends a fatal error message indicating that the client attempted to subscribe
        /// to an event stream with an id that was already in use.
        /// </summary>
        protected virtual Task ErrorIdAlreadyExists(string id)
            => Client.CloseConnectionAsync(4409, $"Subscriber for {id} already exists");

        /// <summary>
        /// Executes when the client is attempting to initalize the connection.
        /// By default this acknowledges the connection via <see cref="OnConnectionAcknowledge(OperationMessage)"/>
        /// and then starts sending keep-alive messages via <see cref="OnSendKeepAlive"/> if configured to do so.
        /// </summary>
        protected virtual async Task OnConnectionInit(OperationMessage message)
        {
            await OnConnectionAcknowledge(message);
            if (_keepAliveTimeout > TimeSpan.Zero)
            {
                _ = Task.Run(async () =>
                {
                    while (true)
                    {
                        await Task.Delay(_keepAliveTimeout, CancellationToken);
                        await OnSendKeepAlive();
                    }
                });
            }
        }

        /// <summary>
        /// Executes when a keep-alive message needs to be sent.
        /// </summary>
        protected abstract Task OnSendKeepAlive();

        /// <summary>
        /// Executes when a connection request needs to be acknowledged.
        /// </summary>
        protected abstract Task OnConnectionAcknowledge(OperationMessage message);

        /// <summary>
        /// Executes when a new subscription request has occurred.
        /// Optionally disconnects any existing subscription associated with the same id.
        /// </summary>
        protected virtual async Task Subscribe(OperationMessage message, bool overwrite)
        {
            if (string.IsNullOrEmpty(message.Id))
            {
                await ErrorIdCannotBeBlank();
                return;
            }

            var dummyDisposer = new DummyDisposer();

            try
            {
                if (overwrite)
                {
                    Subscriptions[message.Id] = dummyDisposer;
                }
                else
                {
                    if (!Subscriptions.TryAdd(message.Id, dummyDisposer))
                    {
                        await ErrorIdAlreadyExists(message.Id);
                        return;
                    }
                }

                var result = await ExecuteRequest(message);
                if (!Subscriptions.Contains(message.Id, dummyDisposer))
                    return;
                if (result is SubscriptionExecutionResult subscriptionExecutionResult && subscriptionExecutionResult.Streams?.Count == 1)
                {
                    // do not return a result, but set up a subscription
                    var stream = subscriptionExecutionResult.Streams.Single().Value;
                    // note that this may immediately trigger some notifications
                    var disposer = stream.Subscribe(new Observer(this, message.Id));
                    try
                    {
                        if (Subscriptions.CompareExchange(message.Id, dummyDisposer, disposer))
                        {
                            disposer = null;
                        }
                    }
                    finally
                    {
                        disposer?.Dispose();
                    }
                }
                else if (result.Executed && result.Data != null)
                {
                    await SendSingleResult(message.Id, result);
                }
                else
                {
                    await SendErrorResult(message.Id, result);
                }
            }
            catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (!Subscriptions.Contains(message.Id, dummyDisposer))
                    return;
                var error = await HandleError(ex);
                await SendErrorResult(message.Id, error);
            }
        }

        /// <summary>
        /// Creates an <see cref="ExecutionError"/> for an unknown <see cref="Exception"/>.
        /// </summary>
        protected virtual Task<ExecutionError> HandleError(Exception ex)
            => Task.FromResult(new ExecutionError("Unable to set up subscription for the requested field.", ex));

        /// <summary>
        /// Sends a single result to the client for a subscription or request, along with a notice
        /// that it was the last result in the event stream.
        /// </summary>
        protected virtual async Task SendSingleResult(string id, ExecutionResult result)
        {
            await SendData(id, result);
            await SendCompleted(id);
        }

        /// <summary>
        /// Sends an execution error to the client during set-up of a subscription.
        /// </summary>
        protected virtual Task SendErrorResult(string id, ExecutionError error)
            => SendErrorResult(id, new ExecutionResult { Errors = new ExecutionErrors { error } });

        /// <summary>
        /// Sends an error result to the client during set-up of a subscription.
        /// </summary>
        protected abstract Task SendErrorResult(string id, ExecutionResult result);

        /// <summary>
        /// Sends a data packet to the client for a subscription event.
        /// </summary>
        protected abstract Task SendData(string id, ExecutionResult result);

        /// <summary>
        /// Sends a notice that a subscription has completed and no more data packets will be sent.
        /// </summary>
        protected abstract Task SendCompleted(string id);

        /// <summary>
        /// Executes a GraphQL request. The request is inside <see cref="OperationMessage.Payload"/>
        /// and will need to be deserialized by <see cref="IGraphQLSerializer.ReadNode{T}(object?)"/>
        /// into a <see cref="GraphQLRequest"/> instance.
        /// </summary>
        protected abstract Task<ExecutionResult> ExecuteRequest(OperationMessage message);

        /// <summary>
        /// Unsubscribes from a subscription event stream.
        /// </summary>
        protected virtual Task Unsubscribe(string id)
        {
            Subscriptions.TryRemove(id);
            return Task.CompletedTask;
        }

        private class Observer : IObserver<ExecutionResult>
        {
            private readonly OperationMessageServer _handler;
            private readonly string _id;

            public Observer(OperationMessageServer handler, string id)
            {
                _handler = handler;
                _id = id;
            }

            public void OnCompleted()
            {
                try
                {
                    _ = _handler.SendCompleted(_id);
                }
                catch
                {
                }
            }

            public void OnError(Exception error)
                => throw new NotSupportedException();

            public void OnNext(ExecutionResult value)
            {
                try
                {
                    _ = _handler.SendData(_id, value);
                }
                catch
                {
                }
            }
        }

        private class DummyDisposer : IDisposable
        {
            public void Dispose() { }
        }
    }
}
