using GraphQL.Server.Common;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.Server.Transports.AspNetCore.Common
{
    /// <summary>
    /// An interface for deserializers of GraphQL requests.
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
        /// <returns>Result containing success flag and deserialized request/s.</returns>
        Task<GraphQLRequestDeserializationResult> DeserializeFromJsonBodyAsync(HttpRequest httpRequest, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deserializes the query string of the request URL, into a <see cref="GraphQLRequest".
        /// </summary>
        /// <param name="queryCollection">Request URL's query collection.</param>
        /// <returns>Deserialized GraphQL request.</returns>
        GraphQLRequest DeserializeFromQueryString(IQueryCollection queryCollection);

        /// <summary>
        /// Deserializes the body of the request, containing 'application/graphql' content,
        /// into a <see cref="GraphQLRequest".
        /// </summary>
        /// <param name="bodyStream">Request body as a stream.</param>
        /// <returns>Deserialized GraphQL request.</returns>
        Task<GraphQLRequest> DeserializeFromGraphBodyAsync(Stream bodyStream);
    }
}
