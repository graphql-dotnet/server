using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace GraphQL.Server.Transports.AspNetCore
{
    public class GraphQLHttpOptions
    {
        public PathString Path { get; set; } = "/graphql";

        public Func<ExecutionOptions, HttpContext, Task> ConfigureAsync { get; set; }
    }
}
