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
        private readonly ILoggerFactory _loggerFactory;
        private SubscriptionServer _server;
        private ILogger<WebSocketConnection> _logger;

        public WebSocketConnection(
            WebSocket socket,
            string connectionId,
            ISubscriptionManager subscriptionManager,
            ILoggerFactory loggerFactory)
        {
            ConnectionId = connectionId;
            _socket = socket;
            _subscriptionManager = subscriptionManager;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<WebSocketConnection>();
        }

        public string ConnectionId { get; }

        protected IMessageTransport GetTransport()
        {
            return new WebSocketTransport(_socket);
        }

        public Task Connect()
        {
            _logger.LogInformation("Creating server for connection {connectionId}", ConnectionId);
            _server = new SubscriptionServer(
                GetTransport(), 
                _subscriptionManager,
                _loggerFactory.CreateLogger<SubscriptionServer>());

            return _server.OnConnected();
        }

        public Task Close()
        {
            _server.Transport.Writer.Complete();
            _server.Transport.Reader.Complete();

            return Task.CompletedTask;
        }
    }
}