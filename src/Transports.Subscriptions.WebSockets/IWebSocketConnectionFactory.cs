using GraphQL.Types;
using System.Net.WebSockets;

namespace GraphQL.Server.Transports.WebSockets
{
    public interface IWebSocketConnectionFactory<TSchema>
        where TSchema : ISchema
    {
        WebSocketConnection CreateConnection(WebSocket socket, string connectionId);
    }
}
