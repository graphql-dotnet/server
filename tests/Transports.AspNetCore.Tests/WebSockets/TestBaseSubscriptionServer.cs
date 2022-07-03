namespace Tests.WebSockets;

public class TestBaseSubscriptionServer : BaseSubscriptionServer
{
    public TestBaseSubscriptionServer(IWebSocketConnection sendStream, GraphQLHttpMiddlewareOptions options)
        : base(sendStream, options.WebSockets, options) { }

    public TestBaseSubscriptionServer(IWebSocketConnection sendStream, GraphQLWebSocketOptions options, IAuthorizationOptions authorizationOptions)
        : base(sendStream, options, authorizationOptions) { }

    public override Task OnMessageReceivedAsync(OperationMessage message) => throw new NotImplementedException();
    protected override Task<ExecutionResult> ExecuteRequestAsync(OperationMessage message) => throw new NotImplementedException();
    protected override Task OnConnectionAcknowledgeAsync(OperationMessage message) => throw new NotImplementedException();
    protected override Task OnSendKeepAliveAsync() => throw new NotImplementedException();
    protected override Task SendCompletedAsync(string id) => throw new NotImplementedException();
    protected override Task SendDataAsync(string id, ExecutionResult result) => throw new NotImplementedException();
    protected override Task SendErrorResultAsync(string id, ExecutionResult result) => throw new NotImplementedException();

    public Task Do_InitializeConnectionAsync() => InitializeConnectionAsync();

    public Task Do_OnConnectionInitWaitTimeoutAsync() => OnConnectionInitWaitTimeoutAsync();

    public SubscriptionList Get_Subscriptions => Subscriptions;

    public bool Get_Initialized => Initialized;

    public bool Do_TryInitialize() => TryInitialize();

    public Task Do_OnCloseConnectionAsync() => OnCloseConnectionAsync();

    public Task Do_ErrorConnectionInitializationTimeoutAsync() => ErrorConnectionInitializationTimeoutAsync();

    public Task Do_ErrorTooManyInitializationRequestsAsync(OperationMessage message)
        => ErrorTooManyInitializationRequestsAsync(message);

    public Task Do_ErrorNotInitializedAsync(OperationMessage message)
        => ErrorNotInitializedAsync(message);

    public Task Do_ErrorUnrecognizedMessageAsync(OperationMessage message)
        => ErrorUnrecognizedMessageAsync(message);

    public Task Do_ErrorIdCannotBeBlankAsync(OperationMessage message)
        => ErrorIdCannotBeBlankAsync(message);

    public Task Do_ErrorIdAlreadyExistsAsync(OperationMessage message)
        => ErrorIdAlreadyExistsAsync(message);

    public Task Do_ErrorAccessDeniedAsync()
        => ErrorAccessDeniedAsync();

    public Task Do_OnConnectionInitAsync(OperationMessage message, bool smartKeepAlive)
        => OnConnectionInitAsync(message, smartKeepAlive);

    public Task Do_SubscribeAsync(OperationMessage message, bool overwrite)
        => SubscribeAsync(message, overwrite);

    public Task<ExecutionError> Do_HandleErrorDuringSubscribeAsync(OperationMessage message, Exception ex)
        => HandleErrorDuringSubscribeAsync(message, ex);

    public Task Do_SendSingleResultAsync(OperationMessage message, ExecutionResult result)
        => SendSingleResultAsync(message, result);

    public Task Do_SendErrorResultAsync(OperationMessage message, ExecutionError error)
        => SendErrorResultAsync(message, error);

    public Task Do_SendErrorResultAsync(string id, ExecutionError error)
        => SendErrorResultAsync(id, error);

    public Task Do_SendErrorResultAsync(OperationMessage message, ExecutionResult result)
        => SendErrorResultAsync(message, result);

    public Task Do_SendErrorResultAsync(string id, ExecutionResult result)
        => SendErrorResultAsync(id, result);

    public Task Do_UnsubscribeAsync(string? id)
        => UnsubscribeAsync(id);

    public ValueTask<bool> Do_AuthorizeAsync(OperationMessage message)
        => AuthorizeAsync(message);

    public Task<ExecutionError> Do_HandleErrorFromSourceAsync(Exception exception)
        => HandleErrorFromSourceAsync(exception);

    public TimeSpan Get_DefaultConnectionTimeout => DefaultConnectionTimeout;
    public TimeSpan Get_DefaultKeepAliveTimeout => DefaultKeepAliveTimeout;
}
