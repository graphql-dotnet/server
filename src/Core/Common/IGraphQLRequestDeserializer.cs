using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace GraphQL.Server.Common
{
    public interface IGraphQLRequestDeserializer
    {
        Task<GraphQLRequestDeserializationResult> DeserializeAsync(HttpRequest httpRequest);
    }
}
