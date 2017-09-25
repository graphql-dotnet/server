using System.Net.WebSockets;
using System.Threading.Tasks;
using GraphQL.Server.Transports.WebSockets.Messages;

namespace GraphQL.Server.Transports.WebSockets
{
    public class GraphQLConnectionContext
    {
        public const string Protocol = "graphql-ws";
        private readonly WebSocketClient _socketClient;

        public GraphQLConnectionContext(WebSocket socket, string connectionId)
        {
            ConnectionId = connectionId;
            _socketClient = new WebSocketClient(socket);
            Reader = new JsonMessageReader(_socketClient);
            Writer = new JsonMessageWriter(_socketClient);
        }

        public string ConnectionId { get; }

        public WebSocketCloseStatus? CloseStatus => _socketClient.CloseStatus;

        public JsonMessageWriter Writer { get; protected set; }

        public JsonMessageReader Reader { get; protected set; }

        public Task CloseAsync()
        {
            return _socketClient.CloseAsync();
        }
    }
}
