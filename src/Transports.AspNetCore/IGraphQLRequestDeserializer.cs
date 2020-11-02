using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Transports.AspNetCore
{
    /// <summary>
    /// An interface for GraphQL request deserializer.
    /// </summary>
    /// <remarks>
    /// Various forms of requests should be supported by a GraphQL HTTP endpoint.
    /// See https://graphql.org/learn/serving-over-http/ for specifics.
    /// </remarks>
    public interface IGraphQLRequestDeserializer
    {
        /// <summary>
        /// Deserializes the body of the <paramref name="httpRequest"/>, containing JSON,
        /// into a <see cref="GraphQLRequestDeserializationResult"/>.
        ///
        /// Supports single or batch GraphQL requests.
        /// </summary>
        /// <param name="httpRequest">Incoming HTTP request.</param>
        /// <param name="cancellationToken">Token to cancel deserialization.</param>
        /// <returns>Result containing success flag and deserialized request/s.</returns>
        Task<GraphQLRequestDeserializationResult> DeserializeFromJsonBodyAsync(HttpRequest httpRequest, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deserializes inputs (a.k.a. variables) from a JSON-encoded string.
        /// </summary>
        /// <param name="json">JSON-encoded string.</param>
        /// <returns>Inputs.</returns>
        Inputs DeserializeInputsFromJson(string json);
    }
}
