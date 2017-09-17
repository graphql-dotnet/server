using System.Threading.Tasks;

namespace GraphQL.Server.Transports.WebSockets.Abstractions
{
    public interface IWebSocketMessageClient
    {
        Task<string> ReadMessageAsync();
        Task WriteMessageAsync(string message);
    }
}
