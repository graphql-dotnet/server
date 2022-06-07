using GraphQL.Transport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server.Transports.AspNetCore.WebSockets.GraphQLWs;

/// <inheritdoc/>
public class SubscriptionServer : BaseSubscriptionServer
{
    private readonly IWebSocketAuthenticationService? _authenticationService;

    /// <summary>
    /// The WebSocket sub-protocol used for this protocol.
    /// </summary>
    public const string SubProtocol = "graphql-transport-ws";

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
    /// Initailizes a new instance with the specified parameters.
    /// </summary>
    /// <param name="sendStream">The WebSockets stream used to send data packets or close the connection.</param>
    /// <param name="options">Configuration options for this instance.</param>
    /// <param name="executer">The <see cref="IDocumentExecuter"/> to use to execute GraphQL requests.</param>
    /// <param name="serializer">The <see cref="IGraphQLSerializer"/> to use to deserialize payloads stored within <see cref="OperationMessage.Payload"/>.</param>
    /// <param name="serviceScopeFactory">A <see cref="IServiceScopeFactory"/> to create service scopes for execution of GraphQL requests.</param>
    /// <param name="userContextBuilder">The user context builder used during connection initialization.</param>
    /// <param name="authenticationService">An optional service to authenticate connections.</param>
    public SubscriptionServer(
        IWebSocketConnection sendStream,
        GraphQLHttpMiddlewareOptions options,
        IDocumentExecuter executer,
        IGraphQLSerializer serializer,
        IServiceScopeFactory serviceScopeFactory,
        IUserContextBuilder userContextBuilder,
        IWebSocketAuthenticationService? authenticationService = null)
        : base(sendStream, options)
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
                await OnConnectionInitAsync(message, true);
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

    /// <summary>
    /// GQL_PONG is a requrired response to a ping, and also a unidirectional keep-alive packet,
    /// whereas GQL_PING is a bidirectional keep-alive packet.
    /// </summary>
    private static readonly OperationMessage _pongMessage = new() { Type = MessageType.Pong };

    /// <summary>
    /// Executes when a ping message is received.
    /// </summary>
    protected virtual Task OnPingAsync(OperationMessage message)
        => Connection.SendMessageAsync(_pongMessage);

    /// <summary>
    /// Executes when a pong message is received.
    /// </summary>
    protected virtual Task OnPongAsync(OperationMessage message)
        => Task.CompletedTask;

    /// <inheritdoc/>
    protected override Task OnSendKeepAliveAsync()
        => Connection.SendMessageAsync(_pongMessage);

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
        var request = Serializer.ReadNode<GraphQLRequest>(message.Payload)!;
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
    /// method to authenticate the request, checks the authorization rules set in <see cref="GraphQLHttpMiddlewareOptions"/>,
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

        var success = await base.AuthorizeAsync(message);

        if (success)
        {
            UserContext = await UserContextBuilder.BuildUserContextAsync(Connection.HttpContext, message.Payload);
        }

        return success;
    }
}
