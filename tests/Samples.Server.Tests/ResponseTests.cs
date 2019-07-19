using GraphQL.Server.Transports.AspNetCore.Common;
using Shouldly;
using System.Threading.Tasks;
using Xunit;

namespace Samples.Server.Tests
{
    public class ResponseTests : BaseTest
    {
        [Fact]
        public async Task Simple_Query_Should_Work()
        {
            var response = await SendRequestAsync(new GraphQLRequest { Query = "{ __schema { queryType { name } } }" });
            response.ShouldBe(@"{""data"":{""__schema"":{""queryType"":{""name"":""ChatQuery""}}}}");
        }

        [Fact]
        public async Task Batched_Query_Should_Work()
        {
            var response = await SendBatchRequestAsync(
                new GraphQLRequest { Query = "query one { __schema { queryType { name } } }", OperationName = "one" },
                new GraphQLRequest { Query = "query two { __schema { queryType { name } } }", OperationName = "two" },
                new GraphQLRequest { Query = "query three { __schema { queryType { name } } }", OperationName = "three" }
                );
            response.ShouldBe(@"[{""data"":{""__schema"":{""queryType"":{""name"":""ChatQuery""}}}},{""data"":{""__schema"":{""queryType"":{""name"":""ChatQuery""}}}},{""data"":{""__schema"":{""queryType"":{""name"":""ChatQuery""}}}}]");
        }
    }
}
