using GraphQL.Server.Transports.Subscriptions.Abstractions;
using System.Threading.Tasks;

namespace GraphQL.Server.Transports.WebSockets
{
    public class WebSocketConnection
    {
        private readonly WebSocketTransport _transport;
        private readonly SubscriptionServer _server;

        public WebSocketConnection(
            WebSocketTransport transport,
            SubscriptionServer subscriptionServer)
        {
            _transport = transport;
            _server = subscriptionServer;
        }

        public async Task Connect()
        {
            await _server.OnConnect().ConfigureAwait(false);
            await _server.OnDisconnect().ConfigureAwait(false);
            await _transport.CloseAsync().ConfigureAwait(false);
        }
    }
}
