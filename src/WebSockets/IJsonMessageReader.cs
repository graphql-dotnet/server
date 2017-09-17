using System.Threading.Tasks;

namespace GraphQL.Server.Transports.WebSockets
{
    public interface IJsonMessageReader
    {
        Task<T> ReadMessageAsync<T>();
    }
}
