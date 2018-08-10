using Microsoft.AspNetCore.Http;

namespace GraphQL.Server
{
    public class GraphQLWebSocketsMiddlewareOptions
    {
        public PathString Path { get; set; } = "/graphql";

        public string AuthorizationPolicyName { get; set; }
    }
}
