using System.Net.WebSockets;
using System.Threading.Tasks;
using GraphQL.Server.Transports.Subscriptions.Abstractions;

namespace GraphQL.Server.Transports.WebSockets
{
    public class WebSocketConnection
    {
        private readonly WebSocket _socket;
        private readonly ISubscriptionManager _subscriptionManager;
        private SubscriptionServer _server;

        public WebSocketConnection(
            WebSocket socket,
            string connectionId,
            ISubscriptionManager subscriptionManager)
        {
            ConnectionId = connectionId;
            _socket = socket;
            _subscriptionManager = subscriptionManager;
        }

        public string ConnectionId { get; }

        protected IMessageTransport GetTransport()
        {
            return new WebSocketTransport(_socket);
        }

        public Task Connect()
        {
            _server = new SubscriptionServer(GetTransport(), _subscriptionManager);
            return _server.ReceiveMessagesAsync();
        }
    }
}