using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace GraphQL.Server.Transports.AspNetCore.Tests
{
    public class GraphQLHttpFacts
    {
        public GraphQLHttpFacts()
        {
            _server = new TestServer(WebHost.CreateDefaultBuilder()
                .UseStartup<TestStartup>());
        }

        private readonly TestServer _server;

        [Fact]
        public async Task invalid_method_should_return_method_not_allowed()
        {
            /* Given */
            var client = _server.CreateClient();

            /* When */
            var result = await client.DeleteAsync("/graphql");

            /* Then */
            Assert.Equal(HttpStatusCode.MethodNotAllowed, result.StatusCode);
        }
    }
}