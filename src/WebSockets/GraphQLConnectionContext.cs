using System.Net.WebSockets;
using System.Threading.Tasks;
using GraphQL.Server.Transports.WebSockets.Abstractions;
using GraphQL.Server.Transports.WebSockets.Messages;

namespace GraphQL.Server.Transports.WebSockets
{
    public class ConnectionContext : IConnectionContext
    {
        public const string Protocol = "graphql-ws";
        private readonly WebSocketClient _socketClient;

        public ConnectionContext(WebSocket socket, string connectionId)
        {
            ConnectionId = connectionId;
            _socketClient = new WebSocketClient(socket);
            Reader = new JsonMessageReader(_socketClient);
            Writer = new JsonMessageWriter(_socketClient);
        }

        public string ConnectionId { get; }

        public WebSocketCloseStatus? CloseStatus => _socketClient.CloseStatus;

        public IJsonMessageWriter Writer { get; protected set; }

        public IJsonMessageReader Reader { get; protected set; }

        public Task CloseAsync()
        {
            return _socketClient.CloseAsync();
        }
    }
}
