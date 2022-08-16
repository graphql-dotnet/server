namespace GraphQL.Server.Transports.AspNetCore.WebSockets.SubscriptionsTransportWs;

/// <inheritdoc/>
public class SubscriptionServer : BaseSubscriptionServer
{
    private readonly IWebSocketAuthenticationService? _authenticationService;

    /// <summary>
    /// The WebSocket sub-protocol used for this protocol.
    /// </summary>
    /// <remarks>
    /// Please note that the correct sub-protocol for the
    /// <see href="https://github.com/apollographql/subscriptions-transport-ws">subscriptions-transport-ws</see>
    /// protocol is <c>graphql-ws</c> as is defined here.
    /// </remarks>
    public static string SubProtocol => "graphql-ws";

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
    }

    /// <inheritdoc/>
    public override async Task OnMessageReceivedAsync(OperationMessage message)
    {
        if (message.Type == MessageType.GQL_CONNECTION_TERMINATE)
        {
            await OnCloseConnectionAsync();
            return;
        }
        else if (message.Type == MessageType.GQL_CONNECTION_INIT)
        {
            if (Initialized)
            {
                await ErrorTooManyInitializationRequestsAsync(message);
            }
            else
            {
                await OnConnectionInitAsync(message, false);
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
            case MessageType.GQL_START:
                await OnStartAsync(message);
                break;
            case MessageType.GQL_STOP:
                await OnStopAsync(message);
                break;
            default:
                await ErrorUnrecognizedMessageAsync(message);
                break;
        }
    }

    private static readonly OperationMessage _keepAliveMessage = new() { Type = MessageType.GQL_CONNECTION_KEEP_ALIVE };
    /// <inheritdoc/>
    protected override Task OnSendKeepAliveAsync()
        => Connection.SendMessageAsync(_keepAliveMessage);

    private static readonly OperationMessage _connectionAckMessage = new() { Type = MessageType.GQL_CONNECTION_ACK };
    /// <inheritdoc/>
    protected override Task OnConnectionAcknowledgeAsync(OperationMessage message)
        => Connection.SendMessageAsync(_connectionAckMessage);

    /// <summary>
    /// Executes when a request is received to start a subscription.
    /// </summary>
    protected virtual Task OnStartAsync(OperationMessage message)
        => SubscribeAsync(message, true);

    /// <summary>
    /// Executes when a request is received to stop a subscription.
    /// </summary>
    protected virtual Task OnStopAsync(OperationMessage message)
        => UnsubscribeAsync(message.Id);

    /// <inheritdoc/>
    protected override async Task SendErrorResultAsync(string id, ExecutionResult result)
    {
        if (Subscriptions.TryRemove(id))
        {
            await Connection.SendMessageAsync(new OperationMessage
            {
                Id = id,
                Type = MessageType.GQL_ERROR,
                Payload = result,
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
                Type = MessageType.GQL_DATA,
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
                Type = MessageType.GQL_COMPLETE,
            });
        }
    }

    /// <inheritdoc/>
    protected override async Task<ExecutionResult> ExecuteRequestAsync(OperationMessage message)
    {
        var request = Serializer.ReadNode<GraphQLRequest>(message.Payload);
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
        if (request == null)
            throw new ArgumentNullException(nameof(message) + "." + nameof(OperationMessage.Payload));
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
        var scope = ServiceScopeFactory.CreateScope();
        try
        {
            var options = new ExecutionOptions
            {
                Query = request.Query,
                Variables = request.Variables,
                Extensions = request.Extensions,
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

    /// <inheritdoc/>
    protected override async Task ErrorAccessDeniedAsync()
    {
        await Connection.SendMessageAsync(new OperationMessage
        {
            Type = MessageType.GQL_CONNECTION_ERROR,
            Payload = "Access denied",
        });
        await base.ErrorAccessDeniedAsync();
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
