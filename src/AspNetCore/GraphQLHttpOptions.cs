using System;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Transports.AspNetCore
{
    public class GraphQLHttpOptions
    {
        public PathString Path { get; set; } = "/graphql";

        public Func<HttpContext, object> BuildUserContext { get; set; }
    }
}
