#pragma warning disable IDE0060 // Remove unused parameter

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Server.Transports.Subscriptions.Abstractions;
using GraphQL.Subscription;
using GraphQL.Transport;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server.Transports.Subscriptions.WebSockets.Shane.New
{
    public interface IOperationMessageStreamFactory
    {
        Task<IOperationMessageClientStream> ConnectAsync(string subProtocol, IOperationMessageServerStream sendStream, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Must be thread-safe.
    /// </summary>
    public interface IOperationMessageClientStream : IDisposable
    {
        /// <summary>
        /// Called when a message is received from the client.
        /// </summary>
        /// <exception cref="OperationCanceledException"/>
        Task OnMessageReceivedAsync(OperationMessage message);
    }

    /// <summary>
    /// Must be thread-safe.
    /// </summary>
    public interface IOperationMessageServerStream
    {
        /// <summary>
        /// Sends a message.
        /// </summary>
        Task SendMessageAsync(OperationMessage message);

        /// <summary>
        /// Closes the WebSocket connection.
        /// </summary>
        Task CloseConnectionAsync();

        /// <summary>
        /// Closes the WebSocket connection with the specified error information.
        /// </summary>
        Task CloseConnectionAsync(int eventId, string description);
    }

    public class V1OMSHandler : OperationMessageServerHandler
    {
        public V1OMSHandler(
            IOperationMessageServerStream sendStream,
            TimeSpan connectionInitWaitTimeout,
            TimeSpan keepAliveTimeout,
            IGraphQLExecuter executer,
            IGraphQLSerializer serializer,
            IServiceScopeFactory serviceScopeFactory,
            IDictionary<string, object> userContext)
            : base(sendStream, connectionInitWaitTimeout, keepAliveTimeout, executer, serializer, serviceScopeFactory, userContext)
        {
        }

        public override async Task OnMessageReceivedAsync(OperationMessage message)
        {
            if (message.Type == MessageType.GQL_CONNECTION_TERMINATE)
            {
                await OnCloseConnection();
                return;
            }
            else if (message.Type == MessageType.GQL_CONNECTION_INIT)
            {
                if (!TryInitialize())
                {
                    await ErrorTooManyInitializationRequests();
                }
                else
                {
                    await OnConnectionInit(message);
                }
                return;
            }
            if (!Initialized)
            {
                await ErrorNotInitialized();
                return;
            }
            switch (message.Type)
            {
                case MessageType.GQL_START:
                    await OnStart(message);
                    break;
                case MessageType.GQL_STOP:
                    await OnStop(message);
                    break;
                default:
                    await ErrorUnrecognizedMessage();
                    break;
            }
        }

        private readonly OperationMessage _keepAliveMessage = new() { Type = MessageType.GQL_CONNECTION_KEEP_ALIVE };
        protected override async Task OnSendKeepAlive()
        {
            await SendStream.SendMessageAsync(_keepAliveMessage);
        }

        protected override async Task OnConnectionAcknowledge(OperationMessage message)
        {
            await SendStream.SendMessageAsync(new OperationMessage
            {
                Type = MessageType.GQL_CONNECTION_ACK,
            });
        }

        protected override Task OnStart(OperationMessage message)
            => base.OnStart(message, true);

        protected override async Task SendSingleResult(string id, IDisposable subscription, ExecutionResult result)
        {
            await SendStream.SendMessageAsync(new OperationMessage
            {
                Id = id,
                Type = MessageType.GQL_DATA,
                Payload = result,
            });
            await SendStream.SendMessageAsync(new OperationMessage
            {
                Id = id,
                Type = MessageType.GQL_COMPLETE,
            });
        }

        protected override async Task SendErrorResult(string id, IDisposable subscription, ExecutionResult result)
        {
            if (!CheckSubscription(id, subscription))
                return;
            await SendStream.SendMessageAsync(new OperationMessage
            {
                Id = id,
                Type = MessageType.GQL_ERROR,
                Payload = result,
            });
            if (CompareExchangeSubscription(id, subscription, null))
                subscription.Dispose();
        }

        protected override async Task SendData(string id, ExecutionResult result)
        {
            if (CheckSubscription(id))
            {
                await SendStream.SendMessageAsync(new OperationMessage
                {
                    Id = id,
                    Type = MessageType.GQL_DATA,
                    Payload = result,
                });
            }
        }

        protected override async Task SendCompleted(string id)
        {
            if (TryRemoveSubscription(id))
            {
                await SendStream.SendMessageAsync(new OperationMessage
                {
                    Id = id,
                    Type = MessageType.GQL_COMPLETE,
                });
            }
        }
    }

    public abstract class OperationMessageServerHandler : IOperationMessageClientStream
    {
        protected IOperationMessageServerStream SendStream { get; }
        private volatile int _initialized = 0;
        private CancellationTokenSource _cancellationTokenSource;
        protected CancellationToken CancellationToken { get; }
        private readonly object _lock = new object();
        private readonly Dictionary<string, IDisposable> _subscriptions = new();
        private readonly TimeSpan _keepAliveTimeout;
        protected IGraphQLExecuter GraphQLExecuter { get; }
        protected IGraphQLSerializer Serializer { get; }
        protected IServiceScopeFactory ServiceScopeFactory { get; }
        protected IDictionary<string, object> UserContext { get; }

        /// <summary>
        /// Initailizes a new instance with the specified parameters.
        /// </summary>
        /// <param name="sendStream">The WebSockets stream used to send data packets or close the connection.</param>
        /// <param name="connectionInitWaitTimeout">The amount of time to wait for a <see cref="MessageType.GQL_CONNECTION_INIT"/> message before terminating the connection. <see cref="Timeout.InfiniteTimeSpan"/> can be used to disable the timeout.</param>
        /// <param name="keepAliveTimeout">The periodic interval to send <see cref="MessageType.GQL_CONNECTION_KEEP_ALIVE"/> messages after sending a <see cref="MessageType.GQL_CONNECTION_ACK"/>. <see cref="Timeout.InfiniteTimeSpan"/> can be used to disable the keep-alive signal.</param>
        /// <param name="executer">The <see cref="IGraphQLExecuter"/> to use to execute GraphQL requests.</param>
        /// <param name="serializer">The <see cref="IGraphQLSerializer"/> to use to deserialize payloads stored within <see cref="OperationMessage.Payload"/>.</param>
        /// <param name="serviceScopeFactory">A <see cref="IServiceScopeFactory"/> to create service scopes for execution of GraphQL requests.</param>
        /// <param name="userContext">The user context to pass to the <see cref="IGraphQLExecuter"/>.</param>
        public OperationMessageServerHandler(
            IOperationMessageServerStream sendStream,
            TimeSpan connectionInitWaitTimeout,
            TimeSpan keepAliveTimeout,
            IGraphQLExecuter executer,
            IGraphQLSerializer serializer,
            IServiceScopeFactory serviceScopeFactory,
            IDictionary<string, object> userContext)
        {
            if (connectionInitWaitTimeout != Timeout.InfiniteTimeSpan && connectionInitWaitTimeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(connectionInitWaitTimeout));
            if (keepAliveTimeout != Timeout.InfiniteTimeSpan && keepAliveTimeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(keepAliveTimeout));
            SendStream = sendStream ?? throw new ArgumentNullException(nameof(sendStream));
            GraphQLExecuter = executer ?? throw new ArgumentNullException(nameof(executer));
            ServiceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            UserContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _cancellationTokenSource = new();
            CancellationToken = _cancellationTokenSource.Token;
            _keepAliveTimeout = keepAliveTimeout;
            if (connectionInitWaitTimeout != Timeout.InfiniteTimeSpan)
            {
                Task.Run(async () =>
                {
                    await Task.Delay(connectionInitWaitTimeout, CancellationToken);
                    if (_initialized == 0)
                        await OnConnectionInitWaitTimeout();
                });
            }
        }

        /// <summary>
        /// Executes once the initialization timeout has expired without receiving a <see cref="MessageType.GQL_CONNECTION_INIT"/> message.
        /// </summary>
        protected virtual Task OnConnectionInitWaitTimeout()
        {
            return SendStream.CloseConnectionAsync(4408, "Connection initialization timeout");
        }

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
                List<IDisposable> subscriptionsToDispose;
                lock (_lock)
                {
                    subscriptionsToDispose = _subscriptions.Values.ToList();
                    _subscriptions.Clear();
                }
                foreach (var disposer in subscriptionsToDispose)
                {
                    disposer.Dispose();
                }
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
        {
            return Interlocked.Exchange(ref _initialized, 1) == 0;
        }

        /// <summary>
        /// Executes when a message has been received from the client.
        /// </summary>
        /// <exception cref="OperationCanceledException"/>
        public abstract Task OnMessageReceivedAsync(OperationMessage message);

        protected virtual Task OnCloseConnection()
            => SendStream.CloseConnectionAsync();

        protected virtual Task ErrorTooManyInitializationRequests()
            => SendStream.CloseConnectionAsync(4429, "Too many initialization requests");

        protected virtual Task ErrorNotInitialized()
            => SendStream.CloseConnectionAsync(4401, "Unauthorized");

        protected virtual Task ErrorUnrecognizedMessage()
            => SendStream.CloseConnectionAsync(4400, "Unrecognized message");

        protected virtual Task ErrorIdCannotBeBlank()
            => SendStream.CloseConnectionAsync(4400, "Id cannot be blank");

        protected virtual Task ErrorIdAlreadyExists(string id)
            => SendStream.CloseConnectionAsync(4409, $"Subscriber for {id} already exists");

        /// <summary>
        /// Acknowledges the <see cref="MessageType.GQL_CONNECTION_INIT"/> message via <see cref="OnConnectionAcknowledge(OperationMessage)"/>
        /// and starts sending <see cref="MessageType.GQL_CONNECTION_KEEP_ALIVE"/> messages via <see cref="OnSendKeepAlive"/>.
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
        /// Executes when a <see cref="MessageType.GQL_START"/> is received.
        /// </summary>
        protected virtual Task OnStart(OperationMessage message)
            => OnStart(message, false);

        /// <summary>
        /// Executes when a new subscription request has occurred.
        /// </summary>
        protected virtual async Task OnStart(OperationMessage message, bool overwrite)
        {
            if (string.IsNullOrEmpty(message.Id))
            {
                await ErrorIdCannotBeBlank();
                return;
            }

            var dummyDisposer = new DummyDisposer();

            try
            {
                bool added = TryAddSubscription(message.Id, dummyDisposer, overwrite);
                if (!added)
                {
                    await ErrorIdAlreadyExists(message.Id);
                    return;
                }

                var request = Serializer.ReadNode<GraphQLRequest>(message.Payload);
                var result = await ExecuteRequest(request);
                if (!CheckSubscription(message.Id, dummyDisposer))
                    return;
                if (result is SubscriptionExecutionResult subscriptionExecutionResult && subscriptionExecutionResult.Streams.Count == 1)
                {
                    // do not return a result, but set up a subscription
                    var stream = subscriptionExecutionResult.Streams.Single().Value;
                    // note that this may immediately trigger some notifications
                    var disposer = stream.Subscribe(new Observer(this, message.Id));
                    try
                    {
                        if (CompareExchangeSubscription(message.Id, dummyDisposer, disposer))
                        {
                            disposer = null;
                        }
                    }
                    finally
                    {
                        disposer?.Dispose();
                    }
                }
                else if (result.Executed || result.Data == null)
                {
                    await SendSingleResult(message.Id, dummyDisposer, result);
                }
                else
                {
                    await SendErrorResult(message.Id, dummyDisposer, result);
                }
            }
            catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                var error = await HandleError(ex);
                await SendErrorResult(message.Id, dummyDisposer, error);
            }
        }

        protected virtual Task<ExecutionError> HandleError(Exception ex)
            => Task.FromResult(new ExecutionError("Unable to set up subscription for the requested field.", ex));

        protected abstract Task SendSingleResult(string id, IDisposable subscription, ExecutionResult result);

        protected virtual Task SendErrorResult(string id, IDisposable subscription, ExecutionError error)
            => SendErrorResult(id, subscription, new ExecutionResult { Errors = new ExecutionErrors { error } });

        protected abstract Task SendErrorResult(string id, IDisposable subscription, ExecutionResult result);

        protected abstract Task SendData(string id, ExecutionResult result);

        protected abstract Task SendCompleted(string id);

        protected virtual async Task<ExecutionResult> ExecuteRequest(GraphQLRequest request)
        {
            using var scope = ServiceScopeFactory.CreateScope();
            return await GraphQLExecuter.ExecuteAsync(request, UserContext, scope.ServiceProvider, CancellationToken);
        }

        protected virtual Task OnStop(OperationMessage message)
        {
            TryRemoveSubscription(message.Id);
            return Task.CompletedTask;
        }

        private class Observer : IObserver<ExecutionResult>
        {
            private readonly OperationMessageServerHandler _handler;
            private readonly string _id;

            public Observer(OperationMessageServerHandler handler, string id)
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

        /// <summary>
        /// Adds a subscription to the internal list, overwriting an existing registration if specified.
        /// When overwriting an existing registration, the old registration is disposed.
        /// </summary>
        /// <exception cref="OperationCanceledException"/>
        protected bool TryAddSubscription(string id, IDisposable subscription, bool overwrite)
        {
            IDisposable oldDisposable = null;
            try
            {
                lock (_lock)
                {
                    CancellationToken.ThrowIfCancellationRequested();
                    if (overwrite)
                    {
                        _subscriptions.TryGetValue(id, out oldDisposable);
                        _subscriptions[id] = subscription;
                        return true;
                    }
                    else
                    {
                        return _subscriptions.TryAdd(id, subscription);
                    }
                }
            }
            finally
            {
                oldDisposable?.Dispose();
            }
        }

        /// <summary>
        /// Validates that the specified subscription is still active.
        /// </summary>
        /// <exception cref="OperationCanceledException"/>
        protected bool CheckSubscription(string id, IDisposable subscription)
        {
            lock (_lock)
            {
                CancellationToken.ThrowIfCancellationRequested();
                return _subscriptions.TryGetValue(id, out var value) && value == subscription;
            }
        }

        /// <summary>
        /// Validates that the specified subscription is still active.
        /// </summary>
        /// <exception cref="OperationCanceledException"/>
        protected bool CheckSubscription(string id)
        {
            lock (_lock)
            {
                CancellationToken.ThrowIfCancellationRequested();
                return _subscriptions.ContainsKey(id);
            }
        }

        /// <summary>
        /// Exchanges the specified subscription with another implementation for the specified id.
        /// Does not dispose of the old subscription. If <paramref name="newSubscription"/> is
        /// <see langword="null"/> then the old subscription is removed and not replaced.
        /// </summary>
        /// <exception cref="OperationCanceledException"/>
        protected bool CompareExchangeSubscription(string id, IDisposable oldSubscription, IDisposable newSubscription)
        {
            lock (_lock)
            {
                CancellationToken.ThrowIfCancellationRequested();
                if (!_subscriptions.TryGetValue(id, out var value) || value != oldSubscription)
                    return false;
                if (newSubscription == null)
                    _subscriptions.Remove(id);
                else
                    _subscriptions[id] = newSubscription;
                return true;
            }
        }

        /// <summary>
        /// Removes the specified subscription and disposes of it.
        /// Returns <see langword="false"/> if no subscription can be found.
        /// </summary>
        protected bool TryRemoveSubscription(string id)
        {
            IDisposable subscription = null;
            try
            {
                lock (_lock)
                {
                    CancellationToken.ThrowIfCancellationRequested();
                    if (_subscriptions.TryGetValue(id, out subscription))
                    {
                        _subscriptions.Remove(id);
                        return true;
                    }
                    return false;
                }
            }
            finally
            {
                subscription?.Dispose();
            }
        }

        private class DummyDisposer : IDisposable
        {
            public void Dispose() { }
        }
    }

    public class SubscriptionManager
    {

    }
}
