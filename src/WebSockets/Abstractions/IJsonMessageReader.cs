using System.Threading.Tasks;

namespace GraphQL.Server.Transports.WebSockets.Abstractions
{
    public interface IJsonMessageReader
    {
        Task<T> ReadMessageAsync<T>();
    }
}
