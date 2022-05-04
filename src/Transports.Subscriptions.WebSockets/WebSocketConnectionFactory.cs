using System.Net.WebSockets;
using GraphQL.Server.Transports.Subscriptions.Abstractions;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GraphQL.Server.Transports.WebSockets;

public class WebSocketConnectionFactory<TSchema> : IWebSocketConnectionFactory<TSchema>
    where TSchema : ISchema
{
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IDocumentExecuter<TSchema> _executer;
    private readonly IEnumerable<IOperationMessageListener> _messageListeners;
    private readonly IGraphQLTextSerializer _serializer;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public WebSocketConnectionFactory(
        ILogger<WebSocketConnectionFactory<TSchema>> logger,
        ILoggerFactory loggerFactory,
        IDocumentExecuter<TSchema> executer,
        IEnumerable<IOperationMessageListener> messageListeners,
        IGraphQLTextSerializer serializer,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _executer = executer;
        _messageListeners = messageListeners;
        _serviceScopeFactory = serviceScopeFactory;
        _serializer = serializer;
    }

    public WebSocketConnection CreateConnection(WebSocket socket, string connectionId)
    {
        _logger.LogDebug("Creating server for connection {connectionId}", connectionId);

        var transport = new WebSocketTransport(socket, _serializer);
        var manager = new SubscriptionManager(_executer, _loggerFactory, _serviceScopeFactory);
        var server = new SubscriptionServer(
            transport,
            manager,
            _messageListeners,
            _loggerFactory.CreateLogger<SubscriptionServer>()
        );

        return new WebSocketConnection(transport, server);
    }
}
