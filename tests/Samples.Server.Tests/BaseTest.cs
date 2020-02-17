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

        protected Task<HttpResponseMessage> SendRequestAsync(HttpMethod httpMethod, HttpContent httpContent)
        {
            var request = new HttpRequestMessage(httpMethod, "graphql")
            {
                Content = httpContent
            };
            return Client.SendAsync(request);
        }

        protected async Task<string> SendRequestAsync(GraphQLRequest request, RequestType requestType,
            GraphQLRequest queryStringOverride = null)
        {
            // Different servings over HTTP:
            // https://graphql.org/learn/serving-over-http/
            // https://github.com/graphql/express-graphql/blob/master/src/index.js

            string url = "graphql";
            if (queryStringOverride != null)
            {
                if (requestType == RequestType.Get)
                {
                    throw new ArgumentException(
                        $"It's not valid to set a {nameof(queryStringOverride)} " +
                        $"with a {nameof(requestType)} of {requestType} as the {nameof(request)} " +
                        $"will already be set in the querystring",
                        nameof(queryStringOverride));
                }

                url += "?" + await Serializer.ToQueryStringParamsAsync(queryStringOverride);
            }

            HttpResponseMessage response;
            switch (requestType)
            {
                case RequestType.Get:
                    var urlWithParams = url + "?" + await Serializer.ToQueryStringParamsAsync(request);
                    response = await Client.GetAsync(urlWithParams);
                    break;

                case RequestType.PostWithJson:
                    var json = Serializer.ToJson(request);
                    var jsonContent = new StringContent(json, Encoding.UTF8, MediaType.Json);
                    response = await Client.PostAsync(url, jsonContent);
                    break;

                case RequestType.PostWithGraph:
                    var graphContent = new StringContent(request.Query, Encoding.UTF8, MediaType.GraphQL);
                    response = await Client.PostAsync(url, graphContent);
                    break;

                case RequestType.PostWithForm:
                    var formContent = Serializer.ToFormUrlEncodedContent(request);
                    response = await Client.PostAsync(url, formContent);
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
