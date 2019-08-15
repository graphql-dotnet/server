using GraphQL.Samples.Server;
using GraphQL.Server.Transports.AspNetCore.Common;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Samples.Server.Tests
{
    public abstract class BaseTest : IDisposable
    {
        protected BaseTest()
        {
            Server = new TestServer(Program.CreateWebHostBuilder(Array.Empty<string>()));
            Client = Server.CreateClient();
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

        public virtual void Dispose()
        {
            Client.Dispose();
            Server.Dispose();
        }

        protected TestServer Server { get; }

        protected HttpClient Client { get; }
    }
}
