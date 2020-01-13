using GraphQL.Server.Transports.AspNetCore.Common;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GraphQL.Server.Serialization.NewtonsoftJson
{
    public class GraphQLRequestDeserializer : IGraphQLRequestDeserializer
    {
        private readonly JsonSerializer _serializer;

        public GraphQLRequestDeserializer(Action<JsonSerializerSettings> configure)
        {
            var settings = new JsonSerializerSettings();
            configure(settings);
            _serializer = JsonSerializer.Create(settings); // it's thread safe https://stackoverflow.com/questions/36186276/is-the-json-net-jsonserializer-threadsafe
        }

        public Task<GraphQLRequestDeserializationResult> FromBodyAsync(Stream stream)
        {
            // Do not explicitly or implicitly (via using, etc.) call dispose because StreamReader will dispose inner stream.
            // This leads to the inability to use the stream further by other consumers/middlewares of the request processing
            // pipeline. In fact, it is absolutely not dangerous not to dispose StreamReader as it does not perform any useful
            // work except for the disposing inner stream.
            var reader = new StreamReader(stream);

            var result = new GraphQLRequestDeserializationResult() { WasSuccessful = true };

            using (var jsonReader = new JsonTextReader(reader) { CloseInput = false })
            {
                switch (reader.Peek())
                {
                    case '{':
                        result.Single = _serializer.Deserialize<GraphQLRequest>(jsonReader);
                        break;
                    case '[':
                        result.Batch = _serializer.Deserialize<GraphQLRequest[]>(jsonReader);
                        break;
                    default:
                        result.WasSuccessful = false;
                        break;
                }
            }

            return Task.FromResult(result);
        }
    }
}
