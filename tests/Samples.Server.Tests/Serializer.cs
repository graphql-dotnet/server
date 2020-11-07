using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using GraphQL.Server.Common;

namespace GraphQL.Server
{
    /// <summary>
    /// Shim and utility methods between our internal <see cref="GraphQLRequest"/> representation
    /// and how it should serialize over the wire.
    /// </summary>
    internal static class Serializer
    {
        internal static string ToJson(GraphQLRequest request)
            => ToJson(request.ToDictionary());

        internal static string ToJson(GraphQLRequest[] requests)
            => ToJson(Array.ConvertAll(requests, r => r.ToDictionary()));

        public static string ToJson(object obj)
            => JsonSerializer.Serialize(obj, new JsonSerializerOptions { IgnoreNullValues = true });

        internal static FormUrlEncodedContent ToFormUrlEncodedContent(GraphQLRequest request)
        {
            // Don't add keys if `null` as they'll be url encoded as "" or "null"

            var dictionary = new Dictionary<string, string>();

            if (request.OperationName != null)
            {
                dictionary[GraphQLRequest.OPERATION_NAME_KEY] = request.OperationName;
            }

            if (request.Query != null)
            {
                dictionary[GraphQLRequest.QUERY_KEY] = request.Query;
            }

            if (request.Inputs != null)
            {
                dictionary[GraphQLRequest.VARIABLES_KEY] = ToJson(request.Inputs);
            }

            return new FormUrlEncodedContent(dictionary);
        }

        internal static Task<string> ToQueryStringParamsAsync(GraphQLRequest request)
            => ToFormUrlEncodedContent(request).ReadAsStringAsync();

        private static Dictionary<string, object> ToDictionary(this GraphQLRequest request)
            =>  new Dictionary<string, object>
            {
                { GraphQLRequest.OPERATION_NAME_KEY, request.OperationName },
                { GraphQLRequest.QUERY_KEY, request.Query },
                { GraphQLRequest.VARIABLES_KEY, request.Inputs }
            };
    }
}
