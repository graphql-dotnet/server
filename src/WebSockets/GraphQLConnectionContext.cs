using System.Net.WebSockets;
using System.Threading.Tasks;

namespace GraphQL.Server.Transports.WebSockets
{
    public class GraphQLConnectionContext
    {
        public string ConnectionId { get; }
        private readonly WebSocketMessageClient _socketClient;

        public const string Protocol = "graphql-ws";

        public GraphQLConnectionContext(WebSocket socket, string connectionId)
        {
            ConnectionId = connectionId;
            _socketClient = new WebSocketMessageClient(socket);
            Reader = new JsonMessageReader(_socketClient);
            Writer = new JsonMessageWriter(_socketClient);
        }

        public WebSocketCloseStatus? CloseStatus => _socketClient.CloseStatus;

        public JsonMessageWriter Writer { get; protected set; }

        public JsonMessageReader Reader { get; protected set; }

        public Task CloseAsync()
        {
            return _socketClient.CloseAsync();
        }
    }
}
