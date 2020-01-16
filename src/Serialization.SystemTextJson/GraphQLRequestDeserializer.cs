using GraphQL.Server.Common;
using System;
using System.Buffers;
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

        public async Task<GraphQLRequestDeserializationResult> FromBodyAsync(Stream stream, long? contentLength)
        {
            int length;
            byte[] jsonBytes;
            var sharedArrayPool = ArrayPool<byte>.Shared;
            bool wasRented = false;
            if (contentLength.HasValue)
            {
                length = (int)contentLength;
                jsonBytes = sharedArrayPool.Rent(length);
                wasRented = true;
                await stream.ReadAsync(jsonBytes, 0, length);
            }
            else
            {
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms).ConfigureAwait(false);
                jsonBytes = ms.ToArray();
                length = jsonBytes.Length;
            }
            try
            {
                return Process(jsonBytes, length);
            }
            finally
            {
                if (wasRented)
                {
                    sharedArrayPool.Return(jsonBytes);
                }
            }
        }

        private GraphQLRequestDeserializationResult Process(byte[] jsonBytes, int length)
        {
            var jsonData = jsonBytes.AsSpan(0, length);

            JsonTokenType tokenType;
            try
            {
                tokenType = GetTokenType(jsonData);
            }
            catch (Exception)
            {
                tokenType = JsonTokenType.None;
            }

            var result = new GraphQLRequestDeserializationResult() { WasSuccessful = true };
            switch (tokenType)
            {
                case JsonTokenType.StartObject:
                    result.Single = JsonSerializer.Deserialize<GraphQLRequest>(jsonData, _serializerOptions);
                    break;
                case JsonTokenType.StartArray:
                    result.Batch = JsonSerializer.Deserialize<GraphQLRequest[]>(jsonData, _serializerOptions);
                    break;
                default:
                    result.WasSuccessful = false;
                    break;
            }
            return result;
        }

        private static JsonTokenType GetTokenType(Span<byte> jsonData)
        {
            var reader = new Utf8JsonReader(jsonData);
            var success = reader.Read();
            return success ? reader.TokenType : JsonTokenType.None;
        }
    }
}
