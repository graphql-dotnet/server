using GraphQL.NewtonsoftJson;
using GraphQL.Server.Common;
using GraphQL.Server.Transports.AspNetCore.Common;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
                        result.Single = ToGraphQLRequest(_serializer.Deserialize<InternalGraphQLRequest>(jsonReader));
                        break;
                    case '[':
                        result.Batch = _serializer.Deserialize<InternalGraphQLRequest[]>(jsonReader)
                            .Select(ToGraphQLRequest)
                            .ToArray();
                        break;
                    default:
                        result.IsSuccessful = false;
                        break;
                }
            }

            return Task.FromResult(result);
        }

        public GraphQLRequest DeserializeFromQueryString(IQueryCollection qs) => ToGraphQLRequest(new InternalGraphQLRequest
        {
            Query = qs.TryGetValue(GraphQLRequest.QueryKey, out var queryValues) ? queryValues[0] : null,
            Variables = qs.TryGetValue(GraphQLRequest.VariablesKey, out var variablesValues) ? JObject.Parse(variablesValues[0]) : null,
            OperationName = qs.TryGetValue(GraphQLRequest.OperationNameKey, out var operationNameValues) ? operationNameValues[0] : null
        });

        public GraphQLRequest DeserializeFromFormBody(IFormCollection fc) => ToGraphQLRequest(new InternalGraphQLRequest
        {
            Query = fc.TryGetValue(GraphQLRequest.QueryKey, out var queryValues) ? queryValues[0] : null,
            Variables = fc.TryGetValue(GraphQLRequest.VariablesKey, out var variablesValue) ? JObject.Parse(variablesValue[0]) : null,
            OperationName = fc.TryGetValue(GraphQLRequest.OperationNameKey, out var operationNameValues) ? operationNameValues[0] : null
        });

        private static GraphQLRequest ToGraphQLRequest(InternalGraphQLRequest internalGraphQLRequest)
            => new GraphQLRequest
            {
                OperationName = internalGraphQLRequest.OperationName,
                Query = internalGraphQLRequest.Query,
                Inputs = internalGraphQLRequest.Variables?.ToInputs() // must return null if not provided, not an empty dictionary
            };
    }
}
