using Shouldly;
using System.Threading.Tasks;
using Xunit;

#if NETCOREAPP2_2
using Newtonsoft.Json.Linq;
using GraphQL.Server.Transports.AspNetCore.NewtonsoftJson;
using GraphQL.NewtonsoftJson;
using GraphQLRequest = GraphQL.Server.Transports.AspNetCore.NewtonsoftJson.GraphQLRequest;
#else
using System.Collections.Generic;
using GraphQL.Server.Transports.AspNetCore.SystemTextJson;
using GraphQL.SystemTextJson;
using GraphQLRequest = GraphQL.Server.Transports.AspNetCore.SystemTextJson.GraphQLRequest;
#endif

namespace Samples.Server.Tests
{
    public class ResponseTests : BaseTest
    {
        [Fact]
        public async Task Single_Query_Should_Return_Single_Result()
        {
            var response = await SendRequestAsync(new GraphQLRequest { Query = "{ __schema { queryType { name } } }" });
            response.ShouldBe(@"{""data"":{""__schema"":{""queryType"":{""name"":""ChatQuery""}}}}", ignoreExtensions: true);
        }

        [Fact]
        public async Task Batched_Query_Should_Return_Multiple_Results()
        {
            var response = await SendBatchRequestAsync(
                new GraphQLRequest { Query = "query one { __schema { queryType { name } } }", OperationName = "one" },
                new GraphQLRequest { Query = "query two { __schema { queryType { name } } }", OperationName = "two" },
                new GraphQLRequest { Query = "query three { __schema { queryType { name } } }", OperationName = "three" }
                );
            response.ShouldBe(@"[{""data"":{""__schema"":{""queryType"":{""name"":""ChatQuery""}}}},{""data"":{""__schema"":{""queryType"":{""name"":""ChatQuery""}}}},{""data"":{""__schema"":{""queryType"":{""name"":""ChatQuery""}}}}]", ignoreExtensions: true);
        }

        [Fact]
        public async Task Wrong_Query_Should_Return_Error()
        {
            var response = await SendRequestAsync("Oops");
            response.ShouldBe(@"{""errors"":[{""message"":""Body text could not be parsed. Body text should start with '{' for normal graphql query or with '[' for batched query.""}]}");
        }

        [Fact]
        public async Task Serializer_Should_Handle_Variables()
        {
            var request = new GraphQLRequest
            {
                Query = "mutation ($content: String!, $fromId: String!, $sentAt: Date!) { addMessage(message: { content: $content, fromId: $fromId, sentAt: $sentAt }) { sentAt, content, from { id } } }",
                Variables = ToVariables(@"{ ""content"": ""some content"", ""sentAt"": ""2020-01-01"", ""fromId"": ""1"" }")
            };
            var response = await SendRequestAsync(request);
            response.ShouldBe(
                @"{""data"":{""addMessage"":{""sentAt"":""2020-01-01"",""content"":""some content"",""from"":{""id"":""1""}}}}",
                ignoreExtensions: true);
        }

        [Fact]
        public async Task Serializer_Should_Handle_Complex_Variable()
        {
            var request = new GraphQLRequest
            {
                Query = "mutation ($msg: MessageInputType!) { addMessage(message: $msg) { sentAt, content, from { id } } }",
                Variables = ToVariables(@"{ ""msg"": { ""content"": ""some content"", ""sentAt"": ""2020-01-01"", ""fromId"": ""1"" } }")
            };
            var response = await SendRequestAsync(request);
            response.ShouldBe(
                @"{""data"":{""addMessage"":{""sentAt"":""2020-01-01"",""content"":""some content"",""from"":{""id"":""1""}}}}",
                ignoreExtensions: true);
        }

        [Fact]
        public async Task Serializer_Should_Handle_Empty_Variables()
        {
            var request = new GraphQLRequest
            {
                Query = "{ __schema { queryType { name } } }",
                Variables = ToVariables("{}")
            };
            var response = await SendRequestAsync(request);
            response.ShouldBe(@"{""data"":{""__schema"":{""queryType"":{""name"":""ChatQuery""}}}}", ignoreExtensions: true);
        }

#if NETCOREAPP2_2
        private JObject ToVariables(string json) => JObject.Parse(json);
#else
        private Dictionary<string, object> ToVariables(string json) => json.ToDictionary();
#endif
    }
}
