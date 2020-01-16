using System.IO;
using System.Threading.Tasks;

namespace GraphQL.Server.Common
{
    public interface IGraphQLRequestDeserializer
    {
        Task<GraphQLRequestDeserializationResult> FromBodyAsync(Stream stream, long? contentLength);
    }
}
