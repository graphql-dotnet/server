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
        private const string GRAPHQL_URL = "graphql";

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
            var request = new HttpRequestMessage(httpMethod, GRAPHQL_URL)
            {
                Content = httpContent
            };
            return Client.SendAsync(request);
        }

        /// <summary>
        /// Sends a gql request to the server.
        /// </summary>
        /// <param name="request">Request details.</param>
        /// <param name="requestType">Request type.</param>
        /// <param name="queryStringOverride">
        /// Optional override request details to be passed via the URL query string.
        /// Used to facilitate testing of query string values over body content.
        /// </param>
        /// <returns>
        /// Raw response as a string of JSON.
        /// </returns>
        protected async Task<string> SendRequestAsync(GraphQLRequest request, RequestType requestType,
            GraphQLRequest queryStringOverride = null)
        {
            // Different servings over HTTP:
            // https://graphql.org/learn/serving-over-http/
            // https://github.com/graphql/express-graphql/blob/master/src/index.js

            // Build a url to call the api with
            var url = GRAPHQL_URL;

            // If query string override request details are provided,
            // use it where valid. For PostWithGraph, this is handled in its own part of the next
            // switch statement as it needs to pass its own query strings for just the `request`.
            if (queryStringOverride != null && requestType != RequestType.PostWithGraph)
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

            string urlWithParams;
            HttpResponseMessage response;

            // Handle different request types as necessary
            switch (requestType)
            {
                case RequestType.Get:
                    // Details passed in query string
                    urlWithParams = url + "?" + await Serializer.ToQueryStringParamsAsync(request);
                    response = await Client.GetAsync(urlWithParams);
                    break;

                case RequestType.PostWithJson:
                    // Details passed in body content as JSON, with url query string params also allowed
                    var json = Serializer.ToJson(request);
                    var jsonContent = new StringContent(json, Encoding.UTF8, MediaType.Json);
                    response = await Client.PostAsync(url, jsonContent);
                    break;

                case RequestType.PostWithGraph:
                    // Query in body content (raw), operationName and variables in query string params,
                    // but take the overrides as a priority to facilitate the tests that use it
                    urlWithParams = GRAPHQL_URL + "?" + await Serializer.ToQueryStringParamsAsync(new GraphQLRequest
                    {
                        Query = queryStringOverride?.Query,
                        OperationName = queryStringOverride?.OperationName ?? request.OperationName,
                        Inputs = queryStringOverride?.Inputs ?? request.Inputs
                    });
                    var graphContent = new StringContent(request.Query, Encoding.UTF8, MediaType.GraphQL);
                    response = await Client.PostAsync(urlWithParams, graphContent);
                    break;

                case RequestType.PostWithForm:
                    // Details passed in form body as form url encoded, with url query string params also allowed
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
