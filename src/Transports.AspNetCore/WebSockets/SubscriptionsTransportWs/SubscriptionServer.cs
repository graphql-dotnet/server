using GraphQL.Transport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server.Transports.AspNetCore.WebSockets.SubscriptionsTransportWs;

/// <inheritdoc/>
public class SubscriptionServer : BaseSubscriptionServer
{
    private readonly IWebSocketAuthenticationService? _authenticationService;

    /// <summary>
    /// The WebSocket sub-protocol used for this protocol.
    /// </summary>
    public const string SubProtocol = "graphql-ws";

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
        => Client.SendMessageAsync(_keepAliveMessage);

    private static readonly OperationMessage _connectionAckMessage = new() { Type = MessageType.GQL_CONNECTION_ACK };
    /// <inheritdoc/>
    protected override Task OnConnectionAcknowledgeAsync(OperationMessage message)
        => Client.SendMessageAsync(_connectionAckMessage);

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
            await Client.SendMessageAsync(new OperationMessage
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
            await Client.SendMessageAsync(new OperationMessage
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
            await Client.SendMessageAsync(new OperationMessage
            {
                Id = id,
                Type = MessageType.GQL_COMPLETE,
            });
        }
    }

    /// <inheritdoc/>
    protected override async Task<ExecutionResult> ExecuteRequestAsync(OperationMessage message)
    {
        var request = Serializer.ReadNode<GraphQLRequest>(message.Payload)!;
        using var scope = ServiceScopeFactory.CreateScope();
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
        }
    }

    /// <inheritdoc/>
    protected override async Task ErrorAccessDeniedAsync()
    {
        await Client.SendMessageAsync(new OperationMessage
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
            await _authenticationService.AuthenticateAsync(Client, SubProtocol, message);

        var success = await base.AuthorizeAsync(message);

        if (success)
        {
            UserContext = await UserContextBuilder.BuildUserContextAsync(Client.HttpContext, message.Payload);
        }

        return success;
    }
}
