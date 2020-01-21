using GraphQL.Server.Transports.AspNetCore.Common;
using Shouldly;
using System.Threading.Tasks;
using Xunit;

#if NETCOREAPP2_2
using GraphQLRequest = GraphQL.Server.Serialization.NewtonsoftJson.GraphQLRequest;
#else
using GraphQLRequest = GraphQL.Server.Serialization.SystemTextJson.GraphQLRequest;
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
                Query = "{ __schema { queryType { name } } }",
                Variables = new GraphQL.Inputs()
                {
                    { "key1", "value" },
                    { "key2", new { innerKey = "innerValue" } }
                }
            };
            var response = await SendRequestAsync(request);
            response.ShouldBe(@"{""data"":{""__schema"":{""queryType"":{""name"":""ChatQuery""}}}}", ignoreExtensions: true);
        }

        [Fact]
        public async Task Serializer_Should_Handle_Empty_Variables()
        {
            var request = new GraphQLRequest
            {
                Query = "{ __schema { queryType { name } } }",
                Variables = new GraphQL.Inputs()
            };
            var response = await SendRequestAsync(request);
            response.ShouldBe(@"{""data"":{""__schema"":{""queryType"":{""name"":""ChatQuery""}}}}", ignoreExtensions: true);
        }
    }
}
