using System.Net.WebSockets;
using System.Threading.Tasks;

namespace GraphQL.Server.Transports.WebSockets.Abstractions
{
    public interface IConnectionContext
    {
        string ConnectionId { get; }
        WebSocketCloseStatus? CloseStatus { get; }
        IJsonMessageWriter Writer { get; }
        IJsonMessageReader Reader { get; }
        Task CloseAsync();
    }
}
