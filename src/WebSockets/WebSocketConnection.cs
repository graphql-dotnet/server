using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using GraphQL.Server.Transports.Subscriptions.Abstractions;
using Microsoft.Extensions.Logging;

namespace GraphQL.Server.Transports.WebSockets
{
    public class WebSocketConnection
    {
        private readonly WebSocket _socket;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly IEnumerable<IOperationMessageListener> _messageListeners;
        private readonly ILoggerFactory _loggerFactory;
        private SubscriptionServer _server;
        private readonly ILogger<WebSocketConnection> _logger;

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

        protected IMessageTransport CreateTransport()
        {
            return new WebSocketTransport(_socket);
        }

        public async Task Connect()
        {
            _logger.LogInformation("Creating server for connection {connectionId}", ConnectionId);
            _server = new SubscriptionServer(
                CreateTransport(), 
                _subscriptionManager,
                _messageListeners,
                _loggerFactory.CreateLogger<SubscriptionServer>());

            await _server.OnConnect();
            await _server.OnDisconnect();
        }

    }
}