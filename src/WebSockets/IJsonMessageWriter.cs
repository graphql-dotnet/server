using System.Threading.Tasks;

namespace GraphQL.Server.Transports.WebSockets
{
    public interface IJsonMessageWriter
    {
        Task WriteMessageAsync<T>(T message);
    }
}
