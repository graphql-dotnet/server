namespace GraphQL.Utilities.Federation;

/// <summary>
/// <see cref="HttpRequest" /> extension methods for checking headers.
/// </summary>
public static class GraphQLHttpRequestExtensions
{
    private const string HEADER_NAME = "apollo-federation-include-trace";
    private const string HEADER_VALUE = "ftv1";

    /// <summary>
    /// Determines if federated tracing is <see href="https://www.apollographql.com/docs/federation/metrics/#how-tracing-data-is-exposed-from-a-subgraph">enabled</see> through HTTP headers.
    /// </summary>
    /// <returns><see langword="true"/> if the 'apollo-federation-include-trace' HTTP header has a value of 'ftv1'</returns>
    public static bool IsApolloFederatedTracingEnabled(this HttpRequest request)
    {
        var headers = request?.Headers;
        if (headers != null && headers.TryGetValue(HEADER_NAME, out var values))
        {
            var value = values.FirstOrDefault();
            return HEADER_VALUE.Equals(value, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }
}
