using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Transports.WebSockets
{
    public class GraphQLWebSocketsOptions
    {
        public PathString Path { get; set; } = "/graphql";
    }
}