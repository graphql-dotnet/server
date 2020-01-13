using GraphQL.Server.Transports.AspNetCore.Common;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace GraphQL.Server.Serialization.SystemTextJson
{
    public class GraphQLRequestDeserializer : IGraphQLRequestDeserializer
    {
        private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions();

        public GraphQLRequestDeserializer(Action<JsonSerializerOptions> configure)
        {
            configure(_serializerOptions);
        }

        public async Task<GraphQLRequestDeserializationResult> FromBodyAsync(Stream stream)
        {
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms).ConfigureAwait(false);
            var jsonBytes = ms.ToArray();

            JsonTokenType tokenType;
            try
            {
                tokenType = GetTokenType(jsonBytes);
            }
            catch (Exception)
            {
                tokenType = JsonTokenType.None;
            }

            var result = new GraphQLRequestDeserializationResult() { WasSuccessful = true };

            switch (tokenType)
            {
                case JsonTokenType.StartObject:
                    result.Single = JsonSerializer.Deserialize<GraphQLRequest>(jsonBytes.AsSpan(), _serializerOptions);
                    break;
                case JsonTokenType.StartArray:
                    result.Batch = JsonSerializer.Deserialize<GraphQLRequest[]>(jsonBytes.AsSpan(), _serializerOptions);
                    break;
                default:
                    result.WasSuccessful = false;
                    break;
            }

            return result;
        }

        private static JsonTokenType GetTokenType(byte[] bytes)
        {
            var reader = new Utf8JsonReader(bytes.AsSpan());
            var success = reader.Read();
            return success ? reader.TokenType : JsonTokenType.None;
        }
    }
}
