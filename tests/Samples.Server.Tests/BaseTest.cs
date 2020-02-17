using GraphQL.Samples.Server;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using System;
using GraphQL.Server.Common;
using GraphQL.Server.Transports.AspNetCore.Common;

#if !NETCOREAPP2_2
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

        protected async Task<string> SendRequestAsync(GraphQLRequest request, RequestType requestType, string requestUri = "graphql")
        {
            // Different servings over HTTP:
            // https://graphql.org/learn/serving-over-http/
            HttpResponseMessage response;
            switch (requestType)
            {
                case RequestType.Get:
                    var queryString = await Serializer.ToQueryStringParamsAsync(request);
                    var url = $"{requestUri}?{queryString}";
                    response = await Client.GetAsync(url);
                    break;

                case RequestType.PostWithJson:
                    var jsonContent = Serializer.ToJson(request);
                    response = await Client.PostAsync(requestUri, new StringContent(jsonContent, Encoding.UTF8, MediaType.Json));
                    break;

                case RequestType.PostWithGraph:
                    response = await Client.PostAsync(requestUri, new StringContent(request.Query, Encoding.UTF8, MediaType.GraphQL));
                    break;

                case RequestType.PostWithForm:
                    response = await Client.PostAsync(requestUri, Serializer.ToFormUrlEncodedContent(request));
                    break;

                default:
                    throw new NotImplementedException();
            }
            
            return await response.Content.ReadAsStringAsync();
        }

        protected async Task<string> SendBatchRequestAsync(params GraphQLRequest[] requests)
        {
            var content = Serializer.ToJson(requests);
            using var response = await Client.PostAsync("graphql", new StringContent(content, Encoding.UTF8, "application/json"));
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
