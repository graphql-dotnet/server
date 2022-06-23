using GraphQL.Server.Transports.AspNetCore.WebSockets.GraphQLWs;

namespace Tests.WebSockets;

public class TestNewSubscriptionServer : SubscriptionServer
{
    public TestNewSubscriptionServer(IWebSocketConnection sendStream, GraphQLHttpMiddlewareOptions options,
        IDocumentExecuter executer, IGraphQLSerializer serializer, IServiceScopeFactory serviceScopeFactory,
        IUserContextBuilder userContextBuilder)
        : base(sendStream, options.WebSockets, options, executer, serializer, serviceScopeFactory, userContextBuilder) { }

    public bool Do_TryInitialize()
        => TryInitialize();

    public Task Do_OnPingAsync(OperationMessage message)
        => OnPingAsync(message);

    public Task Do_OnPongAsync(OperationMessage message)
        => OnPongAsync(message);

    public Task Do_OnSendKeepAliveAsync()
        => OnSendKeepAliveAsync();

    public Task Do_OnConnectionAcknowledgeAsync(OperationMessage message)
        => OnConnectionAcknowledgeAsync(message);

    public Task Do_OnSubscribe(OperationMessage message)
        => OnSubscribeAsync(message);

    public Task Do_OnComplete(OperationMessage message)
        => OnCompleteAsync(message);

    public Task Do_SendErrorResultAsync(string id, ExecutionResult result)
        => SendErrorResultAsync(id, result);

    public Task Do_SendDataAsync(string id, ExecutionResult result)
        => SendDataAsync(id, result);

    public Task Do_SendCompletedAsync(string id)
        => SendCompletedAsync(id);

    public Task<ExecutionResult> Do_ExecuteRequestAsync(OperationMessage message)
        => ExecuteRequestAsync(message);

    public SubscriptionList Get_Subscriptions
        => Subscriptions;

    public IGraphQLSerializer Get_Serializer => Serializer;

    public IDictionary<string, object?>? Get_UserContext => UserContext;

    public void Set_UserContext(IDictionary<string, object?>? userContext) => UserContext = userContext;

    public IUserContextBuilder Get_UserContextBuilder => UserContextBuilder;

    public IDocumentExecuter Get_DocumentExecuter => DocumentExecuter;

    public IServiceScopeFactory Get_ServiceScopeFactory => ServiceScopeFactory;
}
