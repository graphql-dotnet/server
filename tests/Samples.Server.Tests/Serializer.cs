using System.Text.Json;
using GraphQL.Transport;

namespace GraphQL.Server;

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
            dictionary["operationName"] = request.OperationName;
        }

        if (request.Query != null)
        {
            dictionary["query"] = request.Query;
        }

        if (request.Variables != null)
        {
            dictionary["variables"] = ToJson(request.Variables);
        }

        if (request.Extensions != null)
        {
            dictionary["extensions"] = ToJson(request.Extensions);
        }

        return new FormUrlEncodedContent(dictionary);
    }

    internal static Task<string> ToQueryStringParamsAsync(GraphQLRequest request)
        => ToFormUrlEncodedContent(request).ReadAsStringAsync();

    private static Dictionary<string, object> ToDictionary(this GraphQLRequest request)
    {
        var dictionary = new Dictionary<string, object>();

        if (request.OperationName != null)
        {
            dictionary["operationName"] = request.OperationName;
        }

        if (request.Query != null)
        {
            dictionary["query"] = request.Query;
        }

        if (request.Variables != null)
        {
            dictionary["variables"] = request.Variables;
        }

        if (request.Extensions != null)
        {
            dictionary["extensions"] = request.Extensions;
        }

        return dictionary;
    }
}
