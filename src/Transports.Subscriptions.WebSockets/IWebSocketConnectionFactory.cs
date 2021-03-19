using System.Net.WebSockets;
using GraphQL.Types;

namespace GraphQL.Server.Transports.WebSockets
{
    public interface IWebSocketConnectionFactory<TSchema>
        where TSchema : ISchema
    {
        WebSocketConnection CreateConnection(WebSocket socket, string connectionId);
    }
}
