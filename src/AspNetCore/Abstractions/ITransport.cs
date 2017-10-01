using System.Threading.Tasks;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Transports.AspNetCore.Abstractions
{
    public interface ITransport<TSchema> where TSchema: Schema
    {
        bool Accepts(HttpContext context);

        Task OnConnectedAsync(HttpContext context);
    }
}
