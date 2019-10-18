using GraphQL.Samples.Server;
using GraphQL.Server.Transports.AspNetCore.Common;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Hosting;

namespace Samples.Server.Tests
{
    public abstract class BaseTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _factory;

        public BaseTest(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;

#if NETCOREAPP2_2
#else
            // Workaround until GraphQL can swap off Newtonsoft.Json and onto the new MS one.
            // https://github.com/graphql-dotnet/graphql-dotnet/issues/1116
            _factory.Server.AllowSynchronousIO = true;
#endif
        }

        protected async Task<string> SendRequestAsync(string text)
        {
            var response = await Client.PostAsync("graphql", new StringContent(text, Encoding.UTF8, "application/json"));
            return await response.Content.ReadAsStringAsync();
        }

        protected async Task<string> SendRequestAsync(GraphQLRequest request)
        {
            var response = await Client.PostAsync("graphql", new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json"));
            return  await response.Content.ReadAsStringAsync();
        }

        protected async Task<string> SendBatchRequestAsync(params GraphQLRequest[] requests)
        {
            var response = await Client.PostAsync("graphql", new StringContent(JsonConvert.SerializeObject(requests), Encoding.UTF8, "application/json"));
            return await response.Content.ReadAsStringAsync();
        }

        protected HttpClient Client => _factory.CreateClient();
    }
}
