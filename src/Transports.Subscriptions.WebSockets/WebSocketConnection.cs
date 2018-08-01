using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using GraphQL.Server.Transports.Subscriptions.Abstractions;
using Microsoft.Extensions.Logging;

namespace GraphQL.Server.Transports.WebSockets
{
    public class WebSocketConnection
    {
        private readonly ILogger<WebSocketConnection> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IEnumerable<IOperationMessageListener> _messageListeners;
        private readonly WebSocket _socket;
        private readonly ISubscriptionManager _subscriptionManager;

        public WebSocketConnection(
            WebSocket socket,
            string connectionId,
            ISubscriptionManager subscriptionManager,
            IEnumerable<IOperationMessageListener> messageListeners,
            ILoggerFactory loggerFactory)
        {
            ConnectionId = connectionId;
            _socket = socket;
            _subscriptionManager = subscriptionManager;
            _messageListeners = messageListeners;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<WebSocketConnection>();
        }

        public string ConnectionId { get; }

        public async Task Connect()
        {
            _logger.LogDebug("Creating server for connection {connectionId}", ConnectionId);
            var transport = new WebSocketTransport(_socket);
            var server = new SubscriptionServer(
                transport,
                _subscriptionManager,
                _messageListeners,
                _loggerFactory.CreateLogger<SubscriptionServer>());

            await server.OnConnect();
            await server.OnDisconnect();
            await transport.CloseAsync();
        }
    }
}