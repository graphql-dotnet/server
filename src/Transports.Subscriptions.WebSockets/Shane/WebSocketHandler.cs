#nullable enable

using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Server.Transports.AspNetCore;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Server.Transports.Subscriptions.WebSockets.Shane;

public class WebSocketHandler<TSchema> : WebSocketHandler, IWebSocketHandler<TSchema>
    where TSchema : ISchema
{
    public WebSocketHandler(
        IGraphQLSerializer serializer,
        IGraphQLExecuter<TSchema> executer,
        IServiceScopeFactory serviceScopeFactory,
        WebSocketHandlerOptions options)
    : base(serializer, executer, serviceScopeFactory, options)
    {
    }
}

public class WebSocketHandler : IWebSocketHandler
{
    private readonly IGraphQLSerializer _serializer;
    private readonly IGraphQLExecuter _executer;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly WebSocketHandlerOptions _webSocketHandlerOptions;

    public WebSocketHandler(
        IGraphQLSerializer serializer,
        IGraphQLExecuter executer,
        IServiceScopeFactory serviceScopeFactory,
        WebSocketHandlerOptions webSocketHandlerOptions)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _executer = executer ?? throw new ArgumentNullException(nameof(executer));
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        _webSocketHandlerOptions = webSocketHandlerOptions ?? throw new ArgumentNullException(nameof(webSocketHandlerOptions));
    }

    private static readonly IEnumerable<string> _supportedSubProtocols = new List<string>(new[] { "graphql-transport-ws", "graphql-ws" }).AsReadOnly();
    public IEnumerable<string> SupportedSubProtocols => _supportedSubProtocols;

    public Task ExecuteAsync(HttpContext httpContext, WebSocket webSocket, string subProtocol, IDictionary<string, object?> userContext, CancellationToken cancellationToken)
    {
        if (httpContext == null)
            throw new ArgumentNullException(nameof(httpContext));
        if (webSocket == null)
            throw new ArgumentNullException(nameof(webSocket));
        if (userContext == null)
            throw new ArgumentNullException(nameof(userContext));
        var webSocketConnection = new WebSocketConnection(webSocket, _serializer, cancellationToken);
        IOperationMessageReceiveStream? operationMessageReceiveStream = null;
        try
        {
            switch (subProtocol)
            {
                case "graphql-transport-ws":
                    operationMessageReceiveStream = new NewSubscriptionServer(
                        webSocketConnection,
                        _webSocketHandlerOptions.ConnectionInitWaitTimeout,
                        _webSocketHandlerOptions.KeepAliveTimeout,
                        _executer,
                        _serializer,
                        _serviceScopeFactory,
                        userContext);
                    break;
                case "graphql-ws":
                    operationMessageReceiveStream = new OldSubscriptionServer(
                        webSocketConnection,
                        _webSocketHandlerOptions.ConnectionInitWaitTimeout,
                        _webSocketHandlerOptions.KeepAliveTimeout,
                        _executer,
                        _serializer,
                        _serviceScopeFactory,
                        userContext);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(subProtocol));
            }
            return webSocketConnection.ExecuteAsync(operationMessageReceiveStream);
        }
        finally
        {
            operationMessageReceiveStream?.Dispose();
        }
    }
}
