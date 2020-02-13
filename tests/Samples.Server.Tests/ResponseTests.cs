using System.Threading.Tasks;
using Xunit;
using GraphQL.Server.Common;
using GraphQL;

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
            var response = await SendRequestAsync(new GraphQLRequest { Query = "{ __schema { queryType { name } } }" }, requestType);
            response.ShouldBeEquivalentJson(@"{""data"":{""__schema"":{""queryType"":{""name"":""ChatQuery""}}}}", ignoreExtensions: true);
        }

        // TODO: Add test for POST with query params overriding the values
        //[Fact]
        //public async Task Single_Query_Using_GraphQL_MediaType_Should_Return_Single_Result_()
        //{
        //    var response = await SendRequestAsync("{ __schema { queryType { name } } }", "application/graphql");
        //    response.ShouldBeEquivalentJson(@"{""data"":{""__schema"":{""queryType"":{""name"":""ChatQuery""}}}}", ignoreExtensions: true);
        //}

        [Fact]
        public async Task Batched_Query_Should_Return_Multiple_Results()
        {
            var response = await SendBatchRequestAsync(
                new GraphQLRequest { Query = "query one { __schema { queryType { name } } }", OperationName = "one" },
                new GraphQLRequest { Query = "query two { __schema { queryType { name } } }", OperationName = "two" },
                new GraphQLRequest { Query = "query three { __schema { queryType { name } } }", OperationName = "three" }
                );
            response.ShouldBeEquivalentJson(@"[{""data"":{""__schema"":{""queryType"":{""name"":""ChatQuery""}}}},{""data"":{""__schema"":{""queryType"":{""name"":""ChatQuery""}}}},{""data"":{""__schema"":{""queryType"":{""name"":""ChatQuery""}}}}]", ignoreExtensions: true);
        }

        [Fact]
        public async Task Wrong_Query_Should_Return_Error()
        {
            var response = await SendRequestAsync("Oops");
            var expected = @"{""errors"":[{""message"":""Body text could not be parsed. Body text should start with '{' for normal graphql query or with '[' for batched query.""}]}";

            response.ShouldBeEquivalentJson(expected);
        }

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
            var response = await SendRequestAsync(request, requestType);
            response.ShouldBeEquivalentJson(
                @"{""data"":{""addMessage"":{""sentAt"":""2020-01-01"",""content"":""some content"",""from"":{""id"":""1""}}}}",
                ignoreExtensions: true);
        }

        [Theory]
        [InlineData(RequestType.Get)]
        [InlineData(RequestType.PostWithJson)]
        [InlineData(RequestType.PostWithForm)]
        public async Task Serializer_Should_Handle_Variables(RequestType requestType)
        {
            var request = new GraphQLRequest
            {
                Query = "mutation ($content: String!, $fromId: String!, $sentAt: Date!) { addMessage(message: { content: $content, fromId: $fromId, sentAt: $sentAt }) { sentAt, content, from { id } } }",
                Inputs = @"{ ""content"": ""some content"", ""sentAt"": ""2020-01-01"", ""fromId"": ""1"" }".ToInputs()
            };
            var response = await SendRequestAsync(request, requestType);
            response.ShouldBeEquivalentJson(
                @"{""data"":{""addMessage"":{""sentAt"":""2020-01-01"",""content"":""some content"",""from"":{""id"":""1""}}}}",
                ignoreExtensions: true);
        }

        [Theory]
        [InlineData(RequestType.Get)]
        [InlineData(RequestType.PostWithJson)]
        [InlineData(RequestType.PostWithForm)]
        public async Task Serializer_Should_Handle_Complex_Variable(RequestType requestType)
        {
            var request = new GraphQLRequest
            {
                Query = "mutation ($msg: MessageInputType!) { addMessage(message: $msg) { sentAt, content, from { id } } }",
                Inputs = @"{ ""msg"": { ""content"": ""some content"", ""sentAt"": ""2020-01-01"", ""fromId"": ""1"" } }".ToInputs()
            };
            var response = await SendRequestAsync(request, requestType);
            response.ShouldBeEquivalentJson(
                @"{""data"":{""addMessage"":{""sentAt"":""2020-01-01"",""content"":""some content"",""from"":{""id"":""1""}}}}",
                ignoreExtensions: true);
        }

        [Theory]
        [InlineData(RequestType.Get)]
        [InlineData(RequestType.PostWithJson)]
        [InlineData(RequestType.PostWithForm)]
        public async Task Serializer_Should_Handle_Empty_Variables(RequestType requestType)
        {
            var request = new GraphQLRequest
            {
                Query = "{ __schema { queryType { name } } }",
                Inputs = "{}".ToInputs()
            };
            var response = await SendRequestAsync(request, requestType);
            response.ShouldBeEquivalentJson(@"{""data"":{""__schema"":{""queryType"":{""name"":""ChatQuery""}}}}", ignoreExtensions: true);
        }
    }
}
