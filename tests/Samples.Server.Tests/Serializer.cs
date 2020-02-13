using System;
using GraphQL.Server.Common;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

#if NETCOREAPP2_2
using Newtonsoft.Json;
#else
using System.Text.Json;
#endif

namespace Samples.Server.Tests
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
#if NETCOREAPP2_2
            => JsonConvert.SerializeObject(obj, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
#else
            => JsonSerializer.Serialize(obj, new JsonSerializerOptions { IgnoreNullValues = true });
#endif

        internal static FormUrlEncodedContent ToFormUrlEncodedContent(GraphQLRequest request)
        {
            // Don't add keys if `null` as they'll be url encoded as "" or "null"

            var dictionary = new Dictionary<string, string>();

            if (request.OperationName != null)
            {
                dictionary[GraphQLRequest.OperationNameKey] = request.OperationName;
            }

            if (request.Query != null)
            {
                dictionary[GraphQLRequest.QueryKey] = request.Query;
            }

            if (request.Inputs != null)
            {
                dictionary[GraphQLRequest.VariablesKey] = ToJson(request.Inputs);
            }

            return new FormUrlEncodedContent(dictionary);
        }

        internal static Task<string> ToQueryStringParamsAsync(GraphQLRequest request)
            => ToFormUrlEncodedContent(request).ReadAsStringAsync();

        private static Dictionary<string, object> ToDictionary(this GraphQLRequest request)
            =>  new Dictionary<string, object>
            {
                { GraphQLRequest.OperationNameKey, request.OperationName },
                { GraphQLRequest.QueryKey, request.Query },
                { GraphQLRequest.VariablesKey, request.Inputs }
            };
    }
}
