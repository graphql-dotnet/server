using Microsoft.AspNetCore.Http;

namespace GraphQL.Server
{
    public class GraphQLHttpMiddlewareOptions
    {
        public PathString Path { get; set; } = "/graphql";

        public string AuthorizationPolicyName { get; set; }
    }
}
