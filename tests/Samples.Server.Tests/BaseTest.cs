using GraphQL.Samples.Server;
using GraphQL.Server.Transports.AspNetCore.Common;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using System;

#if NETCOREAPP2_2
using Microsoft.AspNetCore.Hosting;
#else
using Microsoft.Extensions.Hosting;
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
                     webBuilder
                         .UseTestServer();
                 })
                 .Start();

            Server = Host.GetTestServer();

            // Workaround until GraphQL can swap off Newtonsoft.Json and onto the new MS one.
            // https://github.com/graphql-dotnet/graphql-dotnet/issues/1116
            Server.AllowSynchronousIO = true;
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
