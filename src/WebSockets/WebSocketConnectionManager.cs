using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading.Tasks;
using GraphQL.Server.Transports.Subscriptions.Abstractions;

namespace GraphQL.Server.Transports.WebSockets
{
    public class WebSocketConnectionManager
    {
        private readonly ISubscriptionManager _subscriptionManager;

        public ConcurrentDictionary<string, WebSocketConnection> Connections { get; } =
            new ConcurrentDictionary<string, WebSocketConnection>();

        public WebSocketConnectionManager(ISubscriptionManager subscriptionManager)
        {
            _subscriptionManager = subscriptionManager;
        }

        public async Task Connect(WebSocket socket, string connectionId)
        {
            var connection = new WebSocketConnection(socket, connectionId, _subscriptionManager);
            await Connect(connection);
            await Disconnect(connection);
        }

        private Task Disconnect(WebSocketConnection connection)
        {
            Connections.TryRemove(connection.ConnectionId, out var _);
            return Task.CompletedTask;
        }

        private Task Connect(WebSocketConnection connection)
        {
            if (!Connections.TryAdd(connection.ConnectionId, connection))
                throw new InvalidOperationException($"Connection({connection.ConnectionId}): already is connected");

            return connection.Connect();
        }
    }
}