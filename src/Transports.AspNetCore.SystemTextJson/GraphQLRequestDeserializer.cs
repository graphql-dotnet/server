using GraphQL.Server.Transports.AspNetCore.Common;
using GraphQL.SystemTextJson;
using Microsoft.AspNetCore.Http;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Text.Json;
using System.Threading.Tasks;
using GraphQLRequestBase = GraphQL.Server.Common.GraphQLRequest;

namespace GraphQL.Server.Transports.AspNetCore.SystemTextJson
{
    /// <summary>
    /// Implementation of an <see cref="IGraphQLRequestDeserializer"/> that uses System.Text.Json;
    /// reading the request body asynchronously and optimally.
    /// </summary>
    /// <remarks>
    /// With thanks to David Fowler (@davidfowl) for his help getting this right.
    /// </remarks>
    public class GraphQLRequestDeserializer : IGraphQLRequestDeserializer
    {
        private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions();

        public GraphQLRequestDeserializer(Action<JsonSerializerOptions> configure)
        {
            _serializerOptions.Converters.Add(new ObjectDictionaryConverter());

            configure(_serializerOptions);
        }

        public async Task<GraphQLRequestDeserializationResult> DeserializeAsync(HttpRequest httpRequest)
        {
            JsonTokenType jsonTokenType;
            try
            {
                jsonTokenType = await PeekJsonTokenType(httpRequest.BodyReader);
            }
            catch (JsonException)
            {
                // Invalid request content, assign None so it falls through to WasSuccessful = false
                jsonTokenType = JsonTokenType.None;
            }

            var result = new GraphQLRequestDeserializationResult() { WasSuccessful = true };
            switch (jsonTokenType)
            {
                case JsonTokenType.StartObject:
                    result.Single = await JsonSerializer.DeserializeAsync<GraphQLRequest>(httpRequest.BodyReader.AsStream(), _serializerOptions);
                    return result;
                case JsonTokenType.StartArray:
                    result.Batch = await JsonSerializer.DeserializeAsync<GraphQLRequest[]>(httpRequest.BodyReader.AsStream(), _serializerOptions);
                    return result;
                default:
                    result.WasSuccessful = false;
                    return result;
            }
        }

        private static async ValueTask<JsonTokenType> PeekJsonTokenType(PipeReader reader)
        {
            // Separate method so that we can use the ref struct
            static bool DetermineTokenType(in ReadOnlySequence<byte> buffer, out JsonTokenType jsonToken)
            {
                var jsonReader = new Utf8JsonReader(buffer);
                if (jsonReader.Read())
                {
                    jsonToken = jsonReader.TokenType;
                    return true;
                }
                jsonToken = JsonTokenType.None;
                return false;
            }

            while (true)
            {
                var result = await reader.ReadAsync();
                var buffer = result.Buffer;

                if (DetermineTokenType(buffer, out var tokenType))
                {
                    // Don't consume any of the buffer so we can re-parse it with the
                    // serializer
                    reader.AdvanceTo(buffer.Start, buffer.Start);
                    return tokenType;
                }
                else
                {
                    // We don't have enough to read a token, keep buffering
                    reader.AdvanceTo(buffer.Start, buffer.End);
                }

                // If there's no more data coming, then bail
                if (result.IsCompleted)
                {
                    return JsonTokenType.None;
                }
            }
        }

        public GraphQLRequestBase Deserialize(IQueryCollection qs) => new GraphQLRequest
        {
            Query = qs.TryGetValue(GraphQLRequestBase.QueryKey, out var queryValues) ? queryValues[0] : null,
            Variables = qs.TryGetValue(GraphQLRequestBase.VariablesKey, out var variablesValues) ? variablesValues[0].ToDictionary() : null,
            OperationName = qs.TryGetValue(GraphQLRequestBase.OperationNameKey, out var operationNameValues) ? operationNameValues[0] : null
        };

        public GraphQLRequestBase Deserialize(IFormCollection fc) => new GraphQLRequest
        {
            Query = fc.TryGetValue(GraphQLRequestBase.QueryKey, out var queryValues) ? queryValues[0] : null,
            Variables = fc.TryGetValue(GraphQLRequestBase.VariablesKey, out var variablesValue) ? variablesValue[0].ToDictionary() : null,
            OperationName = fc.TryGetValue(GraphQLRequestBase.OperationNameKey, out var operationNameValues) ? operationNameValues[0] : null
        };
    }
}
