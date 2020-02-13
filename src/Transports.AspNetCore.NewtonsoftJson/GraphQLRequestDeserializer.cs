using GraphQL.Server.Transports.AspNetCore.Common;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GraphQLRequestBase = GraphQL.Server.Common.GraphQLRequest;

namespace GraphQL.Server.Transports.AspNetCore.NewtonsoftJson
{
    /// <summary>
    /// Implementation of an <see cref="IGraphQLRequestDeserializer"/> that uses Newtonsoft.Json.
    /// </summary>
    public class GraphQLRequestDeserializer : IGraphQLRequestDeserializer
    {
        private readonly JsonSerializer _serializer;

        public GraphQLRequestDeserializer(Action<JsonSerializerSettings> configure)
        {
            var settings = new JsonSerializerSettings();
            configure?.Invoke(settings);
            _serializer = JsonSerializer.Create(settings); // it's thread safe https://stackoverflow.com/questions/36186276/is-the-json-net-jsonserializer-threadsafe
        }

        public Task<GraphQLRequestDeserializationResult> DeserializeFromJsonBodyAsync(HttpRequest httpRequest, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Do not explicitly or implicitly (via using, etc.) call dispose because StreamReader will dispose inner stream.
            // This leads to the inability to use the stream further by other consumers/middlewares of the request processing
            // pipeline. In fact, it is absolutely not dangerous not to dispose StreamReader as it does not perform any useful
            // work except for the disposing inner stream.
            var reader = new StreamReader(httpRequest.Body);

            var result = new GraphQLRequestDeserializationResult { IsSuccessful = true };

            using (var jsonReader = new JsonTextReader(reader) { CloseInput = false })
            {
                var firstChar = reader.Peek();

                cancellationToken.ThrowIfCancellationRequested();

                switch (firstChar)
                {
                    case '{':
                        result.Single = _serializer.Deserialize<GraphQLRequest>(jsonReader);
                        break;
                    case '[':
                        result.Batch = _serializer.Deserialize<GraphQLRequest[]>(jsonReader);
                        break;
                    default:
                        result.IsSuccessful = false;
                        break;
                }
            }

            return Task.FromResult(result);
        }

        public GraphQLRequestBase DeserializeFromQueryString(IQueryCollection qs) => new GraphQLRequest
        {
            Query = qs.TryGetValue(GraphQLRequestBase.QueryKey, out var queryValues) ? queryValues[0] : null,
            Variables = qs.TryGetValue(GraphQLRequestBase.VariablesKey, out var variablesValues) ? JObject.Parse(variablesValues[0]) : null,
            OperationName = qs.TryGetValue(GraphQLRequestBase.OperationNameKey, out var operationNameValues) ? operationNameValues[0] : null
        };

        public GraphQLRequestBase DeserializeFromFormBody(IFormCollection fc) => new GraphQLRequest
        {
            Query = fc.TryGetValue(GraphQLRequestBase.QueryKey, out var queryValues) ? queryValues[0] : null,
            Variables = fc.TryGetValue(GraphQLRequestBase.VariablesKey, out var variablesValue) ? JObject.Parse(variablesValue[0]) : null,
            OperationName = fc.TryGetValue(GraphQLRequestBase.OperationNameKey, out var operationNameValues) ? operationNameValues[0] : null
        };
    }
}
