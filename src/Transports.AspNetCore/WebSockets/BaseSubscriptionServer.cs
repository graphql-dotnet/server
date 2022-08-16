namespace GraphQL.Server.Transports.AspNetCore.WebSockets;

/// <summary>
/// Manages a WebSocket message stream.
/// </summary>
/// <remarks>
/// All methods except <see cref="OnMessageReceivedAsync(OperationMessage)">OnMessageReceivedAsync</see> should be thread-safe.
/// <see cref="OnMessageReceivedAsync(OperationMessage)">OnMessageReceivedAsync</see> will receive an ordered
/// sequence of messages from the client, dispatched by <see cref="IWebSocketConnection"/>.
/// </remarks>
public abstract partial class BaseSubscriptionServer : IOperationMessageProcessor
{
    private int _initialized;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly GraphQLWebSocketOptions _options;
    private readonly IAuthorizationOptions _authorizationOptions;

    /// <summary>
    /// Returns a <see cref="IWebSocketConnection"/> instance that can be used
    /// to send messages to the client.
    /// </summary>
    protected IWebSocketConnection Connection { get; }

    /// <summary>
    /// Returns a <see cref="System.Threading.CancellationToken"/> that is signaled
    /// when this class is disposed or when the WebSockets connection is closed.
    /// Intended to stop background timers.
    /// </summary>
    protected CancellationToken CancellationToken { get; }

    /// <summary>
    /// Returns a synchronized list of subscriptions.
    /// </summary>
    protected SubscriptionList Subscriptions { get; } = new();

    /// <summary>
    /// Returns the default keep-alive timeout.
    /// </summary>
    protected virtual TimeSpan DefaultKeepAliveTimeout { get; } = Timeout.InfiniteTimeSpan;

    /// <summary>
    /// Returns the default connection timeout.
    /// </summary>
    protected virtual TimeSpan DefaultConnectionTimeout { get; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Initializes a new instance with the specified parameters.
    /// </summary>
    /// <param name="connection">The WebSockets stream used to send data packets or close the connection.</param>
    /// <param name="options">Configuration options for this instance.</param>
    /// <param name="authorizationOptions">Authorization options for this instance.</param>
    protected BaseSubscriptionServer(
        IWebSocketConnection connection,
        GraphQLWebSocketOptions options,
        IAuthorizationOptions authorizationOptions)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _authorizationOptions = authorizationOptions ?? throw new ArgumentNullException(nameof(authorizationOptions));
        if (options.ConnectionInitWaitTimeout.HasValue)
        {
            if (options.ConnectionInitWaitTimeout.Value != Timeout.InfiniteTimeSpan && options.ConnectionInitWaitTimeout.Value <= TimeSpan.Zero || options.ConnectionInitWaitTimeout.Value > TimeSpan.FromMilliseconds(int.MaxValue))
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
                throw new ArgumentOutOfRangeException($"{nameof(options)}.{nameof(GraphQLHttpMiddlewareOptions.WebSockets)}.{nameof(GraphQLWebSocketOptions.ConnectionInitWaitTimeout)}");
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
        }
        if (options.KeepAliveTimeout.HasValue)
        {
            if (options.KeepAliveTimeout.Value != Timeout.InfiniteTimeSpan && options.KeepAliveTimeout.Value <= TimeSpan.Zero || options.KeepAliveTimeout.Value > TimeSpan.FromMilliseconds(int.MaxValue))
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
                throw new ArgumentOutOfRangeException($"{nameof(options)}.{nameof(GraphQLHttpMiddlewareOptions.WebSockets)}.{nameof(GraphQLWebSocketOptions.KeepAliveTimeout)}");
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
        }
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(connection.RequestAborted);
        CancellationToken = _cancellationTokenSource.Token;
    }

    /// <inheritdoc/>
    public virtual Task InitializeConnectionAsync()
    {
        var connectInitWaitTimeout = _options.ConnectionInitWaitTimeout ?? DefaultConnectionTimeout;
        if (connectInitWaitTimeout != Timeout.InfiniteTimeSpan)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(connectInitWaitTimeout, CancellationToken); // CancellationToken is set when this class is disposed
                if (!Initialized)
                    await OnConnectionInitWaitTimeoutAsync();
            });
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executes once the initialization timeout has expired without being initialized.
    /// This method may execute concurrently with other code in this class.
    /// </summary>
    protected virtual Task OnConnectionInitWaitTimeoutAsync()
        => ErrorConnectionInitializationTimeoutAsync();

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
            Subscriptions.Dispose();
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Indicates if the connection has been already initialized.
    /// </summary>
    protected bool Initialized
        => Thread.VolatileRead(ref _initialized) == 1;

    /// <summary>
    /// Sets the initialized flag if it has not already been set.
    /// Returns <see langword="false"/> if it was already set.
    /// </summary>
    protected bool TryInitialize()
        => Interlocked.Exchange(ref _initialized, 1) == 0;

    /// <summary>
    /// Executes when a message has been received from the client.
    /// This method should essentially implement a state machine for the implemented protocol.
    /// </summary>
    /// <exception cref="OperationCanceledException"/>
    public abstract Task OnMessageReceivedAsync(OperationMessage message);

    /// <summary>
    /// Executes upon a request to close the connection from the client.
    /// </summary>
    protected virtual Task OnCloseConnectionAsync()
        => Connection.CloseAsync();

    /// <summary>
    /// Executes upon a request that has failed authorization.
    /// </summary>
    protected virtual Task ErrorAccessDeniedAsync()
        => Connection.CloseAsync(4401, "Access denied");

    /// <summary>
    /// Sends a fatal error message indicating that the initialization timeout has expired
    /// without the connection being initialized.
    /// </summary>
    protected virtual Task ErrorConnectionInitializationTimeoutAsync()
        => Connection.CloseAsync(4408, "Connection initialization timeout");

    /// <summary>
    /// Sends a fatal error message indicating that the client attempted to initialize
    /// the connection more than one time.
    /// </summary>
    protected virtual Task ErrorTooManyInitializationRequestsAsync(OperationMessage message)
        => Connection.CloseAsync(4429, "Too many initialization requests");

    /// <summary>
    /// Sends a fatal error message indicating that the client attempted to subscribe
    /// to an event stream before initialization was complete.
    /// </summary>
    protected virtual Task ErrorNotInitializedAsync(OperationMessage message)
        => Connection.CloseAsync(4401, "Unauthorized");

    /// <summary>
    /// Sends a fatal error message indicating that the client attempted to use an
    /// unrecognized message type.
    /// </summary>
    protected virtual Task ErrorUnrecognizedMessageAsync(OperationMessage message)
        => Connection.CloseAsync(4400, "Unrecognized message");

    /// <summary>
    /// Sends a fatal error message indicating that the client attempted to subscribe
    /// to an event stream with an empty id.
    /// </summary>
    protected virtual Task ErrorIdCannotBeBlankAsync(OperationMessage message)
        => Connection.CloseAsync(4400, "Id cannot be blank");

    /// <summary>
    /// Sends a fatal error message indicating that the client attempted to subscribe
    /// to an event stream with an id that was already in use.
    /// </summary>
    protected virtual Task ErrorIdAlreadyExistsAsync(OperationMessage message)
        => Connection.CloseAsync(4409, $"Subscriber for {message.Id} already exists");

    /// <summary>
    /// Authorizes an incoming GraphQL over WebSockets request with the connection initialization message.
    /// <br/><br/>
    /// The default implementation checks the authorization rules set in <see cref="GraphQLHttpMiddlewareOptions"/>,
    /// if any, against <see cref="HttpContext.User"/>.  If validation fails, control is passed
    /// to <see cref="OnNotAuthenticatedAsync(OperationMessage)">OnNotAuthenticatedAsync</see>,
    /// <see cref="OnNotAuthorizedRoleAsync(OperationMessage)">OnNotAuthorizedRoleAsync</see>
    /// or <see cref="OnNotAuthorizedPolicyAsync(OperationMessage, AuthorizationResult)">OnNotAuthorizedPolicyAsync</see>.
    /// <br/><br/>
    /// Derived implementations should call the <see cref="IWebSocketAuthenticationService.AuthenticateAsync(IWebSocketConnection, string, OperationMessage)"/>
    /// method to authenticate the request, and then call this base method.
    /// <br/><br/>
    /// This method will return <see langword="true"/> if authorization is successful, or
    /// return <see langword="false"/> if not.
    /// </summary>
    protected virtual async ValueTask<bool> AuthorizeAsync(OperationMessage message)
    {
        // allocation-free authorization here
        var success = await AuthorizationHelper.AuthorizeAsync(
            new AuthorizationParameters<(BaseSubscriptionServer Server, OperationMessage Message)>(
                Connection.HttpContext,
                _authorizationOptions,
                static info => info.Server.OnNotAuthenticatedAsync(info.Message),
                static info => info.Server.OnNotAuthorizedRoleAsync(info.Message),
                static (info, result) => info.Server.OnNotAuthorizedPolicyAsync(info.Message, result)),
            (this, message));

        return success;
    }

    /// <summary>
    /// Executes if <see cref="GraphQLHttpMiddlewareOptions.AuthorizationRequired"/> is set
    /// but <see cref="IIdentity.IsAuthenticated"/> returns <see langword="false"/>.
    /// <br/><br/>
    /// Default implementation closes the WebSocket connection with a 4401 'Access denied'
    /// error message via <see cref="ErrorAccessDeniedAsync"/>.
    /// </summary>
    protected virtual Task OnNotAuthenticatedAsync(OperationMessage message)
        => ErrorAccessDeniedAsync();

    /// <summary>
    /// Executes if <see cref="GraphQLHttpMiddlewareOptions.AuthorizedRoles"/> is set but
    /// <see cref="ClaimsPrincipal.IsInRole(string)"/> returns <see langword="false"/> for all roles.
    /// <br/><br/>
    /// Default implementation closes the WebSocket connection with a 4401 'Access denied'
    /// error message via <see cref="ErrorAccessDeniedAsync"/>.
    /// </summary>
    protected virtual Task OnNotAuthorizedRoleAsync(OperationMessage message)
        => ErrorAccessDeniedAsync();

    /// <summary>
    /// Executes if <see cref="GraphQLHttpMiddlewareOptions.AuthorizedPolicy"/> is set but
    /// <see cref="IAuthorizationService.AuthorizeAsync(ClaimsPrincipal, object, string)"/>
    /// returns an unsuccessful <see cref="AuthorizationResult"/> for the specified policy.
    /// <br/><br/>
    /// Default implementation closes the WebSocket connection with a 4401 'Access denied'
    /// error message via <see cref="ErrorAccessDeniedAsync"/>.
    /// </summary>
    protected virtual Task OnNotAuthorizedPolicyAsync(OperationMessage message, AuthorizationResult result)
        => ErrorAccessDeniedAsync();

    /// <summary>
    /// Executes when the client is attempting to initialize the connection.
    /// <br/><br/>
    /// By default, this first checks <see cref="AuthorizeAsync(OperationMessage)"/> to validate that the
    /// request has passed authentication.  If validation fails, the connection is closed with an Access
    /// Denied message.
    /// <br/><br/>
    /// Otherwise, the connection is acknowledged via <see cref="OnConnectionAcknowledgeAsync(OperationMessage)"/>,
    /// <see cref="TryInitialize"/> is called to indicate that this WebSocket connection is ready to accept requests,
    /// and keep-alive messages are sent via <see cref="OnSendKeepAliveAsync"/> if configured to do so.
    /// Keep-alive messages are only sent if no messages have been sent over the WebSockets connection for the
    /// length of time configured in <see cref="GraphQLWebSocketOptions.KeepAliveTimeout"/>.
    /// </summary>
    protected virtual async Task OnConnectionInitAsync(OperationMessage message, bool smartKeepAlive)
    {
        if (!await AuthorizeAsync(message))
        {
            return;
        }
        await OnConnectionAcknowledgeAsync(message);
        if (TryInitialize() == false)
            return;

        var keepAliveTimeout = _options.KeepAliveTimeout ?? DefaultKeepAliveTimeout;
        if (keepAliveTimeout > TimeSpan.Zero)
        {
            if (smartKeepAlive)
                _ = StartSmartKeepAliveLoopAsync();
            else
                _ = StartKeepAliveLoopAsync();
        }

        async Task StartKeepAliveLoopAsync()
        {
            while (!CancellationToken.IsCancellationRequested)
            {
                await Task.Delay(keepAliveTimeout, CancellationToken);
                await OnSendKeepAliveAsync();
            }
        }

        /*
         * This 'smart' keep-alive logic doesn't work with subscription-transport-ws because the client will
         * terminate a connection 20 seconds after the last keep-alive was received (or it will never
         * terminate if no keep-alive was ever received).
         *
         * See: https://github.com/graphql/graphql-playground/issues/1247
         */
        async Task StartSmartKeepAliveLoopAsync()
        {
            var lastKeepAliveSent = Connection.LastMessageSentAt;
            while (!CancellationToken.IsCancellationRequested)
            {
                var lastSent = Connection.LastMessageSentAt;
                var lastCommunication = lastKeepAliveSent > lastSent ? lastKeepAliveSent : lastSent;
                var now = DateTime.UtcNow;
                var timePassed = now.Subtract(lastCommunication);
                if (timePassed >= keepAliveTimeout)
                {
                    await OnSendKeepAliveAsync();
                    lastKeepAliveSent = now;
                    await Task.Delay(keepAliveTimeout, CancellationToken);
                }
                else
                {
                    var timeToWait = keepAliveTimeout - timePassed;
                    await Task.Delay(timeToWait, CancellationToken);
                }
            }
        }
    }

    /// <summary>
    /// Executes when a keep-alive message needs to be sent.
    /// </summary>
    protected abstract Task OnSendKeepAliveAsync();

    /// <summary>
    /// Executes when a connection request needs to be acknowledged.
    /// </summary>
    protected abstract Task OnConnectionAcknowledgeAsync(OperationMessage message);

    /// <summary>
    /// Executes when a new GraphQL request (typically subscription) has occurred.
    /// Optionally disconnects any existing subscription associated with the same id.
    /// </summary>
    protected virtual async Task SubscribeAsync(OperationMessage message, bool overwrite)
    {
        if (string.IsNullOrEmpty(message.Id))
        {
            await ErrorIdCannotBeBlankAsync(message);
            return;
        }
        var messageId = message.Id!;

        // 'dummyDisposer' is used as a placeholder between the request to start a subscription
        // and the time that the subscription is set up.  In this manner, if a second 'start subscription'
        // requests arrive within this interim, it will be rejected or overwritten (depending on the
        // value of the overwrite argument) properly.
        var dummyDisposer = new DummyDisposer();

        if (overwrite)
        {
            Subscriptions[messageId] = dummyDisposer;
        }
        else
        {
            if (!Subscriptions.TryAdd(messageId, dummyDisposer))
            {
                await ErrorIdAlreadyExistsAsync(message);
                return;
            }
        }

        // start executing the request without blocking the message loop
        _ = StartSubscribeAsync();

        async Task StartSubscribeAsync()
        {
            try
            {
                var result = await ExecuteRequestAsync(message);
                CancellationToken.ThrowIfCancellationRequested();
                if (!Subscriptions.Contains(messageId, dummyDisposer))
                    return;

                // There is no support within the GraphQL spec for multiple streams to be returned.
                // GraphQL.NET has validation rules to prevent this, but technically the execution
                // strategy still allows for it (if the validation rule were not used).  Regardless, the
                // protocol does not support such a scenario as there would need to be separate
                // IDs for each stream.  So, if there were to be more than one stream returned,
                // execution would fall through to SendErrorResultAsync -- but so long as the
                // validation rules are in place, that should not be possible.
                if (result.Streams?.Count == 1)
                {
                    await SendSubscriptionSuccessfulAsync(message);
                    // do not return a result, but set up a subscription
                    var stream = result.Streams!.Single().Value;
                    // note that this may immediately trigger some notifications
                    var disposer = stream.Subscribe(new Observer(this, messageId, _options.DisconnectAfterErrorEvent, _options.DisconnectAfterAnyError));
                    try
                    {
                        if (Subscriptions.CompareExchange(messageId, dummyDisposer, disposer))
                        {
                            disposer = null;
                        }
                    }
                    finally
                    {
                        disposer?.Dispose();
                    }
                }
                else if (result.Executed)
                {
                    await SendSingleResultAsync(message, result);
                }
                else
                {
                    await SendErrorResultAsync(message, result);
                }
            }
            catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (ExecutionError error)
            {
                if (!Subscriptions.Contains(messageId, dummyDisposer))
                    return;
                await SendErrorResultAsync(message, error);
            }
            catch (Exception ex)
            {
                if (!Subscriptions.Contains(messageId, dummyDisposer))
                    return;
                var error = await HandleErrorDuringSubscribeAsync(message, ex);
                await SendErrorResultAsync(message, error);
            }
        }
    }

    /// <summary>
    /// Creates an <see cref="ExecutionError"/> for an unknown <see cref="Exception"/>.
    /// </summary>
    protected virtual Task<ExecutionError> HandleErrorDuringSubscribeAsync(OperationMessage message, Exception ex)
        => Task.FromResult<ExecutionError>(new UnhandledError("Unable to set up subscription for the requested field.", ex));

    /// <summary>
    /// Sends a single result to the client for a query or mutation request, along with a notice
    /// that it was the last result in the event stream.
    /// </summary>
    protected virtual async Task SendSingleResultAsync(OperationMessage message, ExecutionResult result)
    {
        await SendDataAsync(message.Id!, result);
        await SendCompletedAsync(message.Id!);
    }

    /// <summary>
    /// Sends an execution error to the client during set-up of a GraphQL request (typically subscription).
    /// The default implementation calls <see cref="SendErrorResultAsync(string, ExecutionResult)"/>.
    /// </summary>
    protected virtual Task SendErrorResultAsync(OperationMessage message, ExecutionError executionError)
        => SendErrorResultAsync(message.Id!, new ExecutionResult { Errors = new ExecutionErrors { executionError } });

    /// <summary>
    /// Sends an execution error to the client during set-up of a GraphQL request (typically subscription).
    /// The default implementation calls <see cref="SendErrorResultAsync(string, ExecutionResult)"/>.
    /// </summary>
    protected virtual Task SendErrorResultAsync(string id, ExecutionError executionError)
        => SendErrorResultAsync(id, new ExecutionResult { Errors = new ExecutionErrors { executionError } });

    /// <summary>
    /// Sends an execution error to the client during set-up of a GraphQL request (typically subscription).
    /// The default implementation calls <see cref="SendErrorResultAsync(string, ExecutionResult)"/>.
    /// </summary>
    protected virtual Task SendErrorResultAsync(OperationMessage message, ExecutionResult result)
        => SendErrorResultAsync(message.Id!, result);

    /// <summary>
    /// Sends an execution error to the client during set-up of a GraphQL request (typically subscription).
    /// </summary>
    protected abstract Task SendErrorResultAsync(string id, ExecutionResult result);

    /// <summary>
    /// Sends a notification that the specified subscription has been set up successfully.
    /// Note that both graphql-ws and subscriptions-transport-ws protocols do not need
    /// such notification but some custom protocol may require it; for example, see start_ack message
    /// from https://docs.aws.amazon.com/appsync/latest/devguide/real-time-websocket-client.html
    /// protocol for AWS AppSync. 
    /// </summary>
    protected virtual Task SendSubscriptionSuccessfulAsync(OperationMessage message) => Task.CompletedTask;

    /// <summary>
    /// Sends a data packet to the client for a GraphQL request (typically a subscription event).
    /// </summary>
    protected abstract Task SendDataAsync(string id, ExecutionResult result);

    /// <summary>
    /// Sends a notice that a GraphQL request has completed (typically a subscription) and no more data packets will be sent.
    /// </summary>
    protected abstract Task SendCompletedAsync(string id);

    /// <summary>
    /// Executes a GraphQL request. The request is inside <see cref="OperationMessage.Payload"/>
    /// and will need to be deserialized by <see cref="IGraphQLSerializer.ReadNode{T}(object?)"/>
    /// into a <see cref="GraphQLRequest"/> instance.
    /// </summary>
    protected abstract Task<ExecutionResult> ExecuteRequestAsync(OperationMessage message);

    /// <summary>
    /// Unsubscribes from a subscription event stream.
    /// </summary>
    protected virtual Task UnsubscribeAsync(string? id)
    {
        if (id != null)
            _ = Subscriptions.TryRemove(id);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Wraps an unhandled exception within an <see cref="ExecutionError"/> instance.
    /// </summary>
    protected virtual Task<ExecutionError> HandleErrorFromSourceAsync(Exception exception)
        => Task.FromResult<ExecutionError>(new UnhandledError("Unhandled exception", exception));

    private class DummyDisposer : IDisposable
    {
        public void Dispose() { }
    }
}
