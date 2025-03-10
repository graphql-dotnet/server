namespace GraphQL.Server.Transports.AspNetCore.WebSockets.GraphQLWs;

/// <inheritdoc/>
public class SubscriptionServer : BaseSubscriptionServer
{
    private readonly IWebSocketAuthenticationService? _authenticationService;
    private readonly IGraphQLSerializer _serializer;
    private readonly GraphQLWebSocketOptions _options;
    private DateTime _lastPongReceivedUtc;
    private string? _lastPingId;
    private readonly object _lastPingLock = new();

    /// <summary>
    /// The WebSocket sub-protocol used for this protocol.
    /// </summary>
    /// <remarks>
    /// Please note that the correct sub-protocol for the
    /// <see href="https://github.com/enisdenjo/graphql-ws">graphql-ws</see>
    /// protocol is <c>graphql-transport-ws</c> as is defined here.
    /// </remarks>
    public static string SubProtocol => "graphql-transport-ws";

    /// <summary>
    /// Returns the <see cref="IDocumentExecuter"/> used to execute requests.
    /// </summary>
    protected IDocumentExecuter DocumentExecuter { get; }

    /// <summary>
    /// Returns the <see cref="IServiceScopeFactory"/> used to create a service scope for request execution.
    /// </summary>
    protected IServiceScopeFactory ServiceScopeFactory { get; }

    /// <summary>
    /// Gets or sets the user context used to execute requests.
    /// </summary>
    protected IDictionary<string, object?>? UserContext { get; set; }

    /// <summary>
    /// Returns the user context builder used during connection initialization.
    /// </summary>
    protected IUserContextBuilder UserContextBuilder { get; }

    /// <summary>
    /// Returns the <see cref="IGraphQLSerializer"/> used to deserialize <see cref="OperationMessage"/> payloads.
    /// </summary>
    protected IGraphQLSerializer Serializer { get; }

    /// <summary>
    /// Initializes a new instance with the specified parameters.
    /// </summary>
    /// <param name="connection">The WebSockets stream used to send data packets or close the connection.</param>
    /// <param name="options">Configuration options for this instance.</param>
    /// <param name="authorizationOptions">Authorization options for this instance.</param>
    /// <param name="executer">The <see cref="IDocumentExecuter"/> to use to execute GraphQL requests.</param>
    /// <param name="serializer">The <see cref="IGraphQLSerializer"/> to use to deserialize payloads stored within <see cref="OperationMessage.Payload"/>.</param>
    /// <param name="serviceScopeFactory">A <see cref="IServiceScopeFactory"/> to create service scopes for execution of GraphQL requests.</param>
    /// <param name="userContextBuilder">The user context builder used during connection initialization.</param>
    /// <param name="authenticationService">An optional service to authenticate connections.</param>
    public SubscriptionServer(
        IWebSocketConnection connection,
        GraphQLWebSocketOptions options,
        IAuthorizationOptions authorizationOptions,
        IDocumentExecuter executer,
        IGraphQLSerializer serializer,
        IServiceScopeFactory serviceScopeFactory,
        IUserContextBuilder userContextBuilder,
        IWebSocketAuthenticationService? authenticationService = null)
        : base(connection, options, authorizationOptions)
    {
        DocumentExecuter = executer ?? throw new ArgumentNullException(nameof(executer));
        ServiceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        UserContextBuilder = userContextBuilder ?? throw new ArgumentNullException(nameof(userContextBuilder));
        Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _authenticationService = authenticationService;
        _serializer = serializer;
        _options = options;
    }

    /// <inheritdoc/>
    public override async Task OnMessageReceivedAsync(OperationMessage message)
    {
        if (message.Type == MessageType.Ping)
        {
            await OnPingAsync(message);
            return;
        }
        else if (message.Type == MessageType.Pong)
        {
            await OnPongAsync(message);
            return;
        }
        else if (message.Type == MessageType.ConnectionInit)
        {
            if (Initialized)
            {
                await ErrorTooManyInitializationRequestsAsync(message);
            }
            else
            {
#pragma warning disable CS0618 // Type or member is obsolete
                await OnConnectionInitAsync(message, true);
#pragma warning restore CS0618 // Type or member is obsolete
            }
            return;
        }
        if (!Initialized)
        {
            await ErrorNotInitializedAsync(message);
            return;
        }
        switch (message.Type)
        {
            case MessageType.Subscribe:
                await OnSubscribeAsync(message);
                break;
            case MessageType.Complete:
                await OnCompleteAsync(message);
                break;
            default:
                await ErrorUnrecognizedMessageAsync(message);
                break;
        }
    }

    /// <inheritdoc/>
    [Obsolete($"Please use the {nameof(OnConnectionInitAsync)} and {nameof(OnKeepAliveLoopAsync)} methods instead. This method will be removed in a future version of this library.")]
    protected override Task OnConnectionInitAsync(OperationMessage message, bool smartKeepAlive)
    {
        if (smartKeepAlive)
            return OnConnectionInitAsync(message);
        else
            return base.OnConnectionInitAsync(message, smartKeepAlive);
    }

    /// <inheritdoc/>
    protected override Task OnKeepAliveLoopAsync(TimeSpan keepAliveTimeout, KeepAliveMode keepAliveMode)
    {
        if (keepAliveMode == KeepAliveMode.TimeoutWithPayload)
        {
            if (keepAliveTimeout <= TimeSpan.Zero)
                return Task.CompletedTask;
            return SecureKeepAliveLoopAsync(keepAliveTimeout, keepAliveTimeout);
        }
        return base.OnKeepAliveLoopAsync(keepAliveTimeout, keepAliveMode);

        // pingInterval is the time since the last pong was received before sending a new ping
        // pongInterval is the time to wait for a pong after a ping was sent before forcibly closing the connection
        async Task SecureKeepAliveLoopAsync(TimeSpan pingInterval, TimeSpan pongInterval)
        {
            lock (_lastPingLock)
                _lastPongReceivedUtc = DateTime.UtcNow;
            while (!CancellationToken.IsCancellationRequested)
            {
                // Wait for the next ping interval
                TimeSpan interval;
                var now = DateTime.UtcNow;
                DateTime lastPongReceivedUtc;
                lock (_lastPingLock)
                {
                    lastPongReceivedUtc = _lastPongReceivedUtc;
                }
                var nextPing = lastPongReceivedUtc.Add(pingInterval);
                interval = nextPing.Subtract(now);
                if (interval > TimeSpan.Zero) // could easily be zero or less, if pongInterval is equal or greater than pingInterval
                    await Task.Delay(interval, CancellationToken);

                // Send a new ping message
                await OnSendKeepAliveAsync();

                // Wait for the pong response
                await Task.Delay(pongInterval, CancellationToken);
                bool abort;
                lock (_lastPingLock)
                {
                    abort = _lastPongReceivedUtc == lastPongReceivedUtc;
                }
                if (abort)
                {
                    // Forcibly close the connection if the client has not responded to the keep-alive message.
                    // Do not send a close message to the client or wait for a response.
                    Connection.HttpContext.Abort();
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Pong is a required response to a ping, and also a unidirectional keep-alive packet,
    /// whereas ping is a bidirectional keep-alive packet.
    /// </summary>
    private static readonly OperationMessage _pongMessage = new() { Type = MessageType.Pong };

    /// <summary>
    /// Executes when a ping message is received.
    /// </summary>
    protected virtual Task OnPingAsync(OperationMessage message)
        => message.Payload == null
        ? Connection.SendMessageAsync(_pongMessage)
        : Connection.SendMessageAsync(new OperationMessage { Type = MessageType.Pong, Payload = message.Payload });

    /// <summary>
    /// Executes when a pong message is received.
    /// </summary>
    protected virtual Task OnPongAsync(OperationMessage message)
    {
        if (_options.KeepAliveMode == KeepAliveMode.TimeoutWithPayload)
        {
            try
            {
                var pingId = _serializer.ReadNode<PingPayload>(message.Payload)?.id;
                lock (_lastPingLock)
                {
                    if (_lastPingId == pingId)
                        _lastPongReceivedUtc = DateTime.UtcNow;
                }
            }
            catch { } // ignore deserialization errors in case the pong message does not match the expected format
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override Task OnSendKeepAliveAsync()
    {
        if (_options.KeepAliveMode == KeepAliveMode.TimeoutWithPayload)
        {
            var lastPingId = Guid.NewGuid().ToString("N");
            lock (_lastPingLock)
            {
                _lastPingId = lastPingId;
            }
            return Connection.SendMessageAsync(
                new()
                {
                    Type = MessageType.Ping,
                    Payload = new PingPayload { id = lastPingId }
                }
            );
        }
        else
        {
            return Connection.SendMessageAsync(_pongMessage);
        }
    }

    private static readonly OperationMessage _connectionAckMessage = new() { Type = MessageType.ConnectionAck };
    /// <inheritdoc/>
    protected override Task OnConnectionAcknowledgeAsync(OperationMessage message)
        => Connection.SendMessageAsync(_connectionAckMessage);

    /// <summary>
    /// Executes when a request is received to start a subscription.
    /// </summary>
    protected virtual Task OnSubscribeAsync(OperationMessage message)
        => SubscribeAsync(message, false);

    /// <summary>
    /// Executes when a request is received to stop a subscription.
    /// </summary>
    protected virtual Task OnCompleteAsync(OperationMessage message)
        => UnsubscribeAsync(message.Id);

    /// <inheritdoc/>
    protected override async Task SendErrorResultAsync(string id, ExecutionResult result)
    {
        if (Subscriptions.TryRemove(id))
        {
            await Connection.SendMessageAsync(new OperationMessage
            {
                Id = id,
                Type = MessageType.Error,
                Payload = result.Errors?.ToArray() ?? Array.Empty<ExecutionError>(),
            });
        }
    }

    /// <inheritdoc/>
    protected override async Task SendDataAsync(string id, ExecutionResult result)
    {
        if (Subscriptions.Contains(id))
        {
            await Connection.SendMessageAsync(new OperationMessage
            {
                Id = id,
                Type = MessageType.Next,
                Payload = result,
            });
        }
    }

    /// <inheritdoc/>
    protected override async Task SendCompletedAsync(string id)
    {
        if (Subscriptions.TryRemove(id))
        {
            await Connection.SendMessageAsync(new OperationMessage
            {
                Id = id,
                Type = MessageType.Complete,
            });
        }
    }

    /// <inheritdoc/>
    protected override async Task<ExecutionResult> ExecuteRequestAsync(OperationMessage message)
    {
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
        var request = Serializer.ReadNode<GraphQLRequest>(message.Payload)
            ?? throw new ArgumentNullException(nameof(message) + "." + nameof(OperationMessage.Payload));
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
        var scope = ServiceScopeFactory.CreateScope();
        try
        {
            var options = new ExecutionOptions
            {
                Query = request.Query,
                Variables = request.Variables,
                Extensions = request.Extensions,
                DocumentId = request.DocumentId,
                OperationName = request.OperationName,
                RequestServices = scope.ServiceProvider,
                CancellationToken = CancellationToken,
                User = Connection.HttpContext.User,
            };
            if (UserContext != null)
                options.UserContext = UserContext;
            return await DocumentExecuter.ExecuteAsync(options);
        }
        finally
        {
            if (scope is IAsyncDisposable ad)
                await ad.DisposeAsync();
            else
                scope.Dispose();
        }
    }

    /// <summary>
    /// Authorizes an incoming GraphQL over WebSockets request with the connection initialization message and initializes the <see cref="UserContext"/>.
    /// <br/><br/>
    /// The default implementation calls the <see cref="IWebSocketAuthenticationService.AuthenticateAsync(IWebSocketConnection, string, OperationMessage)"/>
    /// method to authenticate the request (if <see cref="IWebSocketAuthenticationService"/> was specified),
    /// checks the authorization rules set in <see cref="GraphQLHttpMiddlewareOptions"/>,
    /// if any, against <see cref="HttpContext.User"/>.  If validation fails, control is passed
    /// to <see cref="BaseSubscriptionServer.OnNotAuthenticatedAsync(OperationMessage)">OnNotAuthenticatedAsync</see>,
    /// <see cref="BaseSubscriptionServer.OnNotAuthorizedRoleAsync(OperationMessage)">OnNotAuthorizedRoleAsync</see>
    /// or <see cref="BaseSubscriptionServer.OnNotAuthorizedPolicyAsync(OperationMessage, AuthorizationResult)">OnNotAuthorizedPolicyAsync</see>.
    /// <br/><br/>
    /// After successful authorization, the default implementation calls
    /// <see cref="UserContextBuilder"/>.<see cref="IUserContextBuilder.BuildUserContextAsync(HttpContext, object?)">BuildUserContextAsync</see>
    /// to generate a user context.
    /// <br/><br/>
    /// This method will return <see langword="true"/> if authorization is successful, or
    /// return <see langword="false"/> if not.
    /// </summary>
    protected override async ValueTask<bool> AuthorizeAsync(OperationMessage message)
    {
        if (_authenticationService != null)
            await _authenticationService.AuthenticateAsync(Connection, SubProtocol, message);

        bool success = await base.AuthorizeAsync(message);

        if (success)
        {
            UserContext = await UserContextBuilder.BuildUserContextAsync(Connection.HttpContext, message.Payload);
        }

        return success;
    }
}
