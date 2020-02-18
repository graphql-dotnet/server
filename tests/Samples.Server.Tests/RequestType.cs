namespace Samples.Server.Tests
{
    /// <summary>
    /// Different types of HTTP requests a GraphQL HTTP server should be able to understand.
    /// See: https://graphql.org/learn/serving-over-http/
    /// </summary>
    public enum RequestType
    {
        Get,
        PostWithJson,
        PostWithForm,
        PostWithGraph,
    }
}
