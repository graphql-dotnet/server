using System.Threading.Tasks;

namespace GraphQL.Server.Transports.WebSockets.Abstractions
{
    public interface IJsonMessageWriter
    {
        Task WriteMessageAsync<T>(T message);
    }
}
