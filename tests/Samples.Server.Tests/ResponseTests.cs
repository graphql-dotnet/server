using System.Threading.Tasks;
using Xunit;
using GraphQL;
using Shouldly;
using System.Net.Http;
using System.Text;
using System.Net;
using System.Collections.Generic;
using GraphQL.Server;

#if NETCOREAPP2_2
using GraphQL.NewtonsoftJson;
#else
using GraphQL.SystemTextJson;
#endif

namespace Samples.Server.Tests
{
    public class ResponseTests : BaseTest
    {
        [Theory]
        [InlineData(RequestType.Get)]
        [InlineData(RequestType.PostWithJson)]
        [InlineData(RequestType.PostWithGraph)]
        [InlineData(RequestType.PostWithForm)]
        public async Task Single_Query_Should_Return_Single_Result(RequestType requestType)
        {
            var request = new GraphQLRequest { Query = "{ __schema { queryType { name } } }" };
            string response = await SendRequestAsync(request, requestType);
            response.ShouldBeEquivalentJson(@"{""data"":{""__schema"":{""queryType"":{""name"":""ChatQuery""}}}}", ignoreExtensions: true);
        }

        /// <summary>
        /// Tests that POST type requests are overridden by query string params. 
        /// </summary>
        [Theory]
        [InlineData(RequestType.PostWithJson)]
        [InlineData(RequestType.PostWithGraph)]
        [InlineData(RequestType.PostWithForm)]
        public async Task Middleware_Should_Prioritise_Query_String_Values(RequestType requestType)
        {
            var request = new GraphQLRequest
            {
                Query = "mutation one ($content: String!, $fromId: String!, $sentAt: Date!) { addMessage(message: { content: $content, fromId: $fromId, sentAt: $sentAt }) { sentAt, content, from { id } } }",
                Inputs = @"{ ""content"": ""one content"", ""sentAt"": ""2020-01-01"", ""fromId"": ""1"" }".ToInputs(),
                OperationName = "one"
            };

            var requestB = new GraphQLRequest
            {
                Query = "mutation two ($content: String!, $fromId: String!, $sentAt: Date!) { addMessage(message: { content: $content, fromId: $fromId, sentAt: $sentAt }) { sentAt, content, from { id } } }",
                Inputs = @"{ ""content"": ""two content"", ""sentAt"": ""2020-01-01"", ""fromId"": ""1"" }".ToInputs(),
                OperationName = "two"
            };

            string response = await SendRequestAsync(request, requestType, queryStringOverride: requestB);
            response.ShouldBeEquivalentJson(
                @"{""data"":{""addMessage"":{""sentAt"":""2020-01-01"",""content"":""two content"",""from"":{""id"":""1""}}}}",
                ignoreExtensions: true);
        }

        [Fact]
        public async Task Batched_Query_Should_Return_Multiple_Results()
        {
            string response = await SendBatchRequestAsync(
                new GraphQLRequest { Query = "query one { __schema { queryType { name } } }", OperationName = "one" },
                new GraphQLRequest { Query = "query two { __schema { queryType { name } } }", OperationName = "two" },
                new GraphQLRequest { Query = "query three { __schema { queryType { name } } }", OperationName = "three" }
                );
            response.ShouldBeEquivalentJson(@"[{""data"":{""__schema"":{""queryType"":{""name"":""ChatQuery""}}}},{""data"":{""__schema"":{""queryType"":{""name"":""ChatQuery""}}}},{""data"":{""__schema"":{""queryType"":{""name"":""ChatQuery""}}}}]", ignoreExtensions: true);
        }

        [Theory]
        [MemberData(nameof(WrongQueryData))]
        public async Task Wrong_Query_Should_Return_Error(HttpMethod httpMethod, HttpContent httpContent,
            HttpStatusCode expectedStatusCode, string expectedErrorMsg)
        {
            var response = await SendRequestAsync(httpMethod, httpContent);
            string expected = @"{""errors"":[{""message"":""" + expectedErrorMsg + @"""}]}";
            
            response.StatusCode.ShouldBe(expectedStatusCode);

            string content = await response.Content.ReadAsStringAsync();
            content.ShouldBeEquivalentJson(expected);
        }

        public static IEnumerable<object[]> WrongQueryData => new object[][]
        {
            // Methods other than GET or POST shouldn't be allowed
            new object[]
            {
                HttpMethod.Put,
                new StringContent(Serializer.ToJson(new GraphQLRequest { Query = "query { __schema { queryType { name } } }" }), Encoding.UTF8, "application/json"),
                HttpStatusCode.MethodNotAllowed,
                "Invalid HTTP method. Only GET and POST are supported. See: http://graphql.org/learn/serving-over-http/.",
            },

            // POST with an invalid mimetype should be a bad request
            // I couldn't manage to hit this, asp.net core kept rejecting it too early

            // POST with unsupported mimetype should be a bad request
            new object[]
            {
                HttpMethod.Post,
                new StringContent(Serializer.ToJson(new GraphQLRequest { Query = "query { __schema { queryType { name } } }" }), Encoding.UTF8, "something/unknown"),
                HttpStatusCode.BadRequest,
                "Invalid 'Content-Type' header: non-supported media type. Must be of 'application/json', 'application/graphql' or 'application/x-www-form-urlencoded'. See: http://graphql.org/learn/serving-over-http/."
            },

            // POST with JSON mimetype that doesn't start with an object or array token should be a bad request
            new object[]
            {
                HttpMethod.Post,
                new StringContent("Oops", Encoding.UTF8, "application/json"),
                HttpStatusCode.BadRequest,
                "Body text could not be parsed. Body text should start with '{' for normal graphql query or with '[' for batched query."
            }
        };

        [Theory]
        [InlineData(RequestType.Get)]
        [InlineData(RequestType.PostWithJson)]
        [InlineData(RequestType.PostWithGraph)]
        [InlineData(RequestType.PostWithForm)]
        public async Task Serializer_Should_Handle_Inline_Variables(RequestType requestType)
        {
            var request = new GraphQLRequest
            {
                Query = @"mutation { addMessage(message: { content: ""some content"", fromId: ""1"", sentAt: ""2020-01-01"" }) { sentAt, content, from { id } } }"
            };
            string response = await SendRequestAsync(request, requestType);
            response.ShouldBeEquivalentJson(
                @"{""data"":{""addMessage"":{""sentAt"":""2020-01-01"",""content"":""some content"",""from"":{""id"":""1""}}}}",
                ignoreExtensions: true);
        }

        [Theory]
        [InlineData(RequestType.Get)]
        [InlineData(RequestType.PostWithJson)]
        [InlineData(RequestType.PostWithGraph)]
        [InlineData(RequestType.PostWithForm)]
        public async Task Serializer_Should_Handle_Variables(RequestType requestType)
        {
            var request = new GraphQLRequest
            {
                Query = "mutation ($content: String!, $fromId: String!, $sentAt: Date!) { addMessage(message: { content: $content, fromId: $fromId, sentAt: $sentAt }) { sentAt, content, from { id } } }",
                Inputs = @"{ ""content"": ""some content"", ""sentAt"": ""2020-01-01"", ""fromId"": ""1"" }".ToInputs()
            };
            string response = await SendRequestAsync(request, requestType);
            response.ShouldBeEquivalentJson(
                @"{""data"":{""addMessage"":{""sentAt"":""2020-01-01"",""content"":""some content"",""from"":{""id"":""1""}}}}",
                ignoreExtensions: true);
        }

        [Theory]
        [InlineData(RequestType.Get)]
        [InlineData(RequestType.PostWithJson)]
        [InlineData(RequestType.PostWithGraph)]
        [InlineData(RequestType.PostWithForm)]
        public async Task Serializer_Should_Handle_Complex_Variable(RequestType requestType)
        {
            var request = new GraphQLRequest
            {
                Query = "mutation ($msg: MessageInputType!) { addMessage(message: $msg) { sentAt, content, from { id } } }",
                Inputs = @"{ ""msg"": { ""content"": ""some content"", ""sentAt"": ""2020-01-01"", ""fromId"": ""1"" } }".ToInputs()
            };
            string response = await SendRequestAsync(request, requestType);
            response.ShouldBeEquivalentJson(
                @"{""data"":{""addMessage"":{""sentAt"":""2020-01-01"",""content"":""some content"",""from"":{""id"":""1""}}}}",
                ignoreExtensions: true);
        }

        [Theory]
        [InlineData(RequestType.Get)]
        [InlineData(RequestType.PostWithJson)]
        [InlineData(RequestType.PostWithGraph)]
        [InlineData(RequestType.PostWithForm)]
        public async Task Serializer_Should_Handle_Empty_Variables(RequestType requestType)
        {
            var request = new GraphQLRequest
            {
                Query = "{ __schema { queryType { name } } }",
                Inputs = "{}".ToInputs()
            };
            string response = await SendRequestAsync(request, requestType);
            response.ShouldBeEquivalentJson(@"{""data"":{""__schema"":{""queryType"":{""name"":""ChatQuery""}}}}", ignoreExtensions: true);
        }
    }
}
