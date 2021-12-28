using System;
using System.Threading.Tasks;
using GraphQL.Server.Transports.Subscriptions.Abstractions;

namespace GraphQL.Server.Transports.WebSockets
{
    public class WebSocketConnection : IDisposable
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

        public virtual async Task Connect()
        {
            await _server.OnConnect();
            await _server.OnDisconnect();
            await _transport.CloseAsync();
        }

        public virtual void Dispose()
        {
            _server.Dispose();
            _transport.Dispose();
        }
    }
}
