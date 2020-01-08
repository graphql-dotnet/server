using System.IO;
using System.Threading.Tasks;

namespace GraphQL.Server.Transports.AspNetCore.Common
{
    public interface IGraphQLRequestDeserializer
    {
        IGraphQLRequest Default();

        Task<GraphQLRequestDeserializationResult> FromBodyAsync(Stream stream);
    }
}
