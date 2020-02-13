using GraphQL.Server.Common;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.Server.Transports.AspNetCore.Common
{
    /// <summary>
    /// Abstract base deserializer of GraphQL requests, implementing any shared logic.
    /// </summary>
    /// <remarks>
    /// Various forms of requests should be supported by a GraphQL HTTP endpoint.
    /// See https://graphql.org/learn/serving-over-http/ for specifics.
    /// </remarks>
    public abstract class GraphQLRequestDeserializerBase : IGraphQLRequestDeserializer
    {
        public abstract Task<GraphQLRequestDeserializationResult> DeserializeFromJsonBodyAsync(HttpRequest httpRequest, CancellationToken cancellationToken = default);

        public abstract GraphQLRequest DeserializeFromQueryString(IQueryCollection queryCollection);

        public async Task<GraphQLRequest> DeserializeFromGraphBodyAsync(Stream bodyStream)
        {
            // "If the "application/graphql" Content-Type header is present, treat the HTTP POST body contents as the GraphQL query string.",
            // Source: link above.

            // Do not explicitly or implicitly (via using, etc.) call dispose because StreamReader will dispose inner stream.
            // This leads to the inability to use the stream further by other consumers/middlewares of the request processing
            // pipeline. In fact, it is absolutely not dangerous not to dispose StreamReader as it does not perform any useful
            // work except for the disposing inner stream.
            var query = await new StreamReader(bodyStream).ReadToEndAsync().ConfigureAwait(false);

            return new GraphQLRequest { Query = query };
        }
    }
}
