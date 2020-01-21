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
        /// <summary>
        /// Max size of a buffer we can create using the Shared ArrayPool.
        /// </summary>
        /// <remarks>
        /// Determined by following <see cref="ArrayPool{T}.Shared" /> (https://github.com/dotnet/coreclr/blob/master/src/System.Private.CoreLib/shared/System/Buffers/ArrayPool.cs)
        /// which instantiates a TlsOverPerCoreLockedStacksArrayPool (https://github.com/dotnet/runtime/blob/ccf6aedb63c37ea8e10e4f5b5d9d23a69bdd9489/src/libraries/System.Private.CoreLib/src/System/Buffers/TlsOverPerCoreLockedStacksArrayPool.cs)
        /// whose ctor calls Utilities.GetMaxBucketSize with TlsOverPerCoreLockedStacksArrayPool.NumBuckets = 17 (https://github.com/dotnet/runtime/blob/ccf6aedb63c37ea8e10e4f5b5d9d23a69bdd9489/src/libraries/System.Private.CoreLib/src/System/Buffers/Utilities.cs#L22)
        /// which does 16 << 17 = 2MB as below.
        /// </remarks>
        private const int MaxSharedBufferSize = 2097152; 

        private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions();

        public GraphQLRequestDeserializer(Action<JsonSerializerOptions> configure)
        {
            configure(_serializerOptions);
        }

        public async Task<GraphQLRequestDeserializationResult> FromBodyAsync(Stream stream, long? contentLength)
        {
            var sharedArrayPool = ArrayPool<byte>.Shared;
            if (contentLength.HasValue && contentLength < MaxSharedBufferSize)
            {
                var length = (int)contentLength.Value; // MaxSharedBufferSize < int.MaxValue so this is OK
                var jsonBytes = sharedArrayPool.Rent(length); 
                await stream.ReadAsync(jsonBytes, 0, length);

                try
                {
                    return Process(jsonBytes.AsSpan(0, length));
                }
                finally
                {
                    sharedArrayPool.Return(jsonBytes);
                }
            }
            else
            {
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms).ConfigureAwait(false);
                var jsonBytes = ms.GetBuffer();
                return Process(jsonBytes.AsSpan(0, ms.Length));
            }
        }

        private GraphQLRequestDeserializationResult Process(ReadOnlySpan<byte> jsonBytes)
        {
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
                    result.Single = JsonSerializer.Deserialize<GraphQLRequest>(jsonBytes, _serializerOptions);
                    break;
                case JsonTokenType.StartArray:
                    result.Batch = JsonSerializer.Deserialize<GraphQLRequest[]>(jsonBytes, _serializerOptions);
                    break;
                default:
                    result.WasSuccessful = false;
                    break;
            }
            return result;
        }

        private static JsonTokenType GetTokenType(ReadOnlySpan<byte> jsonBytes)
        {
            var reader = new Utf8JsonReader(jsonBytes);
            var success = reader.Read();
            return success ? reader.TokenType : JsonTokenType.None;
        }
    }
}
