using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.NewtonsoftJson;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;

namespace GraphQL.Server.Transports.AspNetCore.NewtonsoftJson
{
    /// <summary>
    /// Implementation of an <see cref="IGraphQLRequestDeserializer"/> that uses Newtonsoft.Json.
    /// </summary>
    public class GraphQLRequestDeserializer : IGraphQLRequestDeserializer
    {
        // https://github.com/dotnet/aspnetcore/blob/main/src/Mvc/Mvc.NewtonsoftJson/src/MvcNewtonsoftJsonOptions.cs
        private const int MemoryBufferThreshold = 1024 * 30;

        private readonly JsonSerializer _serializer;

        public GraphQLRequestDeserializer(Action<JsonSerializerSettings> configure)
        {
            var settings = new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateParseHandling = DateParseHandling.None,
                Converters =
                {
                    new InputsConverter()
                },
            };
            configure?.Invoke(settings);
            _serializer = JsonSerializer.Create(settings); // it's thread safe https://stackoverflow.com/questions/36186276/is-the-json-net-jsonserializer-threadsafe
        }

        public async Task<GraphQLRequestDeserializationResult> DeserializeFromJsonBodyAsync(HttpRequest httpRequest, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // From: https://github.com/dotnet/aspnetcore/blob/main/src/Mvc/Mvc.NewtonsoftJson/src/NewtonsoftJsonInputFormatter.cs
            await using (var readStream = new FileBufferingReadStream(httpRequest.Body, MemoryBufferThreshold))
            {
                await readStream.DrainAsync(cancellationToken);
                readStream.Seek(0L, SeekOrigin.Begin);

                // Do not explicitly or implicitly (via using, etc.) call dispose because StreamReader will dispose inner stream.
                // This leads to the inability to use the stream further by other consumers/middlewares of the request processing
                // pipeline. In fact, it is absolutely not dangerous not to dispose StreamReader as it does not perform any useful
                // work except for the disposing inner stream.
                var reader = new StreamReader(readStream);

                var result = new GraphQLRequestDeserializationResult { IsSuccessful = true };

                using (var jsonReader = new JsonTextReader(reader) { CloseInput = false })
                {
                    int firstChar = reader.Peek();

                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
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
                                result.Exception = GraphQLRequestDeserializationException.InvalidFirstChar();
                                break;
                        }
                    }
                    catch (JsonException e)
                    {
                        result.IsSuccessful = false;
                        result.Exception = new GraphQLRequestDeserializationException(e);
                    }
                }

                return result;
            }
        }

        public Inputs DeserializeInputsFromJson(string json) => json?.ToInputs();

        private static GraphQLRequest ToGraphQLRequest(InternalGraphQLRequest internalGraphQLRequest)
            => new GraphQLRequest
            {
                OperationName = internalGraphQLRequest.OperationName,
                Query = internalGraphQLRequest.Query,
                Inputs = internalGraphQLRequest.Variables, // must return null if not provided, not an empty Inputs
                Extensions = internalGraphQLRequest.Extensions, // must return null if not provided, not an empty Inputs
            };
    }
}
