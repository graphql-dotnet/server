using System.Net.WebSockets;

namespace Tests.WebSockets;

public class TestWebSocketConnection : WebSocketConnection
{
    public TestWebSocketConnection(
        HttpContext httpContext,
        WebSocket webSocket,
        IGraphQLSerializer serializer,
        GraphQLHttpMiddlewareOptions options,
        CancellationToken cancellationToken)
        : base(httpContext, webSocket, serializer, options.WebSockets, cancellationToken)
    {
    }

    public Task Do_OnDispatchMessageAsync(IOperationMessageProcessor operationMessageReceiveStream, OperationMessage message)
        => OnDispatchMessageAsync(operationMessageReceiveStream, message);

    public Task Do_OnSendMessageAsync(OperationMessage message)
        => OnSendMessageAsync(message);

    public Task Do_OnCloseOutputAsync(WebSocketCloseStatus closeStatus, string? closeDescription)
        => OnCloseOutputAsync(closeStatus, closeDescription);

    public TimeSpan Get_DefaultDisconnectionTimeout
        => DefaultDisconnectionTimeout;
}
