using GraphQL.Samples.Server;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using System;
#if NETCOREAPP2_2
using GraphQLRequest = GraphQL.Server.Transports.AspNetCore.NewtonsoftJson.GraphQLRequest;
#else
using Microsoft.Extensions.Hosting;
using GraphQLRequest = GraphQL.Server.Transports.AspNetCore.SystemTextJson.GraphQLRequest;
#endif

namespace Samples.Server.Tests
{
    public abstract class BaseTest : IDisposable
    {
        protected BaseTest()
        {
#if NETCOREAPP2_2
            Server = new TestServer(Program.CreateWebHostBuilder(Array.Empty<string>()));
#else
            Host = Program.CreateHostBuilder(Array.Empty<string>())
                 .ConfigureWebHost(webBuilder =>
                 {
                     webBuilder.UseTestServer();
                 })
                 .Start();

            Server = Host.GetTestServer();
#endif

            Client = Server.CreateClient();
        }

        protected async Task<string> SendRequestAsync(string text)
        {
            var response = await Client.PostAsync("graphql", new StringContent(text, Encoding.UTF8, "application/json"));
            return await response.Content.ReadAsStringAsync();
        }

        protected async Task<string> SendRequestAsync(GraphQLRequest request)
        {
            var content = Serializer.Serialize(request);
            var response = await Client.PostAsync("graphql", new StringContent(content, Encoding.UTF8, "application/json"));
            return  await response.Content.ReadAsStringAsync();
        }

        protected async Task<string> SendBatchRequestAsync(params GraphQLRequest[] requests)
        {
            var content = Serializer.Serialize(requests);
            var response = await Client.PostAsync("graphql", new StringContent(content, Encoding.UTF8, "application/json"));
            return await response.Content.ReadAsStringAsync();
        }

        public virtual void Dispose()
        {
            Client.Dispose();
            Server.Dispose();
#if !NETCOREAPP2_2
            Host.Dispose();
#endif
        }

        private TestServer Server { get; }

        private HttpClient Client { get; }

#if !NETCOREAPP2_2
        private IHost Host { get; }
#endif
    }
}
