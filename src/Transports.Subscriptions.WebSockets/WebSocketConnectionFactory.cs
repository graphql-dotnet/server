using System.Collections.Generic;
using System.Net.WebSockets;
using GraphQL.Server.Internal;
using GraphQL.Server.Transports.Subscriptions.Abstractions;
using GraphQL.Types;
using Microsoft.Extensions.Logging;

namespace GraphQL.Server.Transports.WebSockets
{
    public class WebSocketConnectionFactory<TSchema> : IWebSocketConnectionFactory<TSchema>
        where TSchema : ISchema
    {
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IGraphQLExecuter<TSchema> _executer;
        private readonly IEnumerable<IOperationMessageListener> _messageListeners;

        public WebSocketConnectionFactory(ILogger<WebSocketConnectionFactory<TSchema>> logger,
            ILoggerFactory loggerFactory,
            IGraphQLExecuter<TSchema> executer,
            IEnumerable<IOperationMessageListener> messageListeners)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _executer = executer;
            _messageListeners = messageListeners;
        }

        public WebSocketConnection CreateConnection(WebSocket socket, string connectionId)
        {
            _logger.LogDebug("Creating server for connection {connectionId}", connectionId);

            var transport = new WebSocketTransport(socket);
            var manager = new SubscriptionManager(_executer, _loggerFactory);
            var server = new SubscriptionServer(
                transport,
                manager,
                _messageListeners,
                _loggerFactory.CreateLogger<SubscriptionServer>()
            );

            return new WebSocketConnection(transport, server);
        }
    }
}
