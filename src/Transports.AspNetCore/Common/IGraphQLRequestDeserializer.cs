using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace GraphQL.Server.Transports.AspNetCore.Common
{
    public interface IGraphQLRequestDeserializer
    {
        Task<GraphQLRequestDeserializationResult> DeserializeAsync(HttpRequest httpRequest);
    }
}
