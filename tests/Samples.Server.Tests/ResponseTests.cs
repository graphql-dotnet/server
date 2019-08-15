using GraphQL.Server.Transports.AspNetCore.Common;
using Shouldly;
using System.Threading.Tasks;
using Xunit;

namespace Samples.Server.Tests
{
    public class ResponseTests : BaseTest
    {
        [Fact]
        public async Task Single_Query_Should_Return_Single_Result()
        {
            var response = await SendRequestAsync(new GraphQLRequest { Query = "{ __schema { queryType { name } } }" });
            response.ShouldBe(@"{""data"":{""__schema"":{""queryType"":{""name"":""ChatQuery""}}}}");
        }

        [Fact]
        public async Task Batched_Query_Should_Return_Multiple_Results()
        {
            var response = await SendBatchRequestAsync(
                new GraphQLRequest { Query = "query one { __schema { queryType { name } } }", OperationName = "one" },
                new GraphQLRequest { Query = "query two { __schema { queryType { name } } }", OperationName = "two" },
                new GraphQLRequest { Query = "query three { __schema { queryType { name } } }", OperationName = "three" }
                );
            response.ShouldBe(@"[{""data"":{""__schema"":{""queryType"":{""name"":""ChatQuery""}}}},{""data"":{""__schema"":{""queryType"":{""name"":""ChatQuery""}}}},{""data"":{""__schema"":{""queryType"":{""name"":""ChatQuery""}}}}]");
        }

        [Fact]
        public async Task Wrong_Query_Should_Return_Error()
        {
            var response = await SendRequestAsync("Oops");
            response.ShouldBe(@"{""errors"":[{""message"":""Body text could not be parsed. Body text should start with '{' for normal graphql query or with '[' for batched query.""}]}");
        }
    }
}
