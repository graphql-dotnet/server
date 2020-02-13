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
    /// Shim between our internal <see cref="GraphQLRequest"/> representation
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
            => JsonConvert.SerializeObject(obj, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
#else
            => JsonSerializer.Serialize(obj, new JsonSerializerOptions { IgnoreNullValues = true });
#endif

        internal static Task<string> ToUrlEncodedStringAsync(GraphQLRequest request)
        {
            var dictionary = new Dictionary<string, string>
            {
                { "operationName", request.OperationName },
                { "query", request.Query },
                { "variables", ToJson(request.Inputs) }
            };
            return new FormUrlEncodedContent(dictionary).ReadAsStringAsync();
        }

        private static Dictionary<string, object> ToDictionary(this GraphQLRequest request)
            => new Dictionary<string, object>
            {
                { "operationName", request.OperationName },
                { "query", request.Query },
                { "variables", request.Inputs }
            };
    }
}
