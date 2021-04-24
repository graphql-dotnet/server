using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace GraphQL.Server.Transports.AspNetCore.Tests
{
    public partial class SystemTextJsonTests
    {
        [Fact]
        public async Task Decodes_Request()
        {
            var request = @"{""query"":""abc"",""operationName"":""def"",""variables"":{""a"":""b"",""c"":2},""extensions"":{""d"":""e"",""f"":3}}";
            var ret = await Deserialize(request);
            ret.IsSuccessful.ShouldBeTrue();
            ret.Single.Query.ShouldBe("abc");
            ret.Single.OperationName.ShouldBe("def");
            ret.Single.Inputs["a"].ShouldBeOfType<string>().ShouldBe("b");
            ret.Single.Inputs["c"].ShouldBeOfType<int>().ShouldBe(2);
            ret.Single.Extensions["d"].ShouldBeOfType<string>().ShouldBe("e");
            ret.Single.Extensions["f"].ShouldBeOfType<int>().ShouldBe(3);
        }

        [Fact]
        public async Task Decodes_Empty_Request()
        {
            var request = @"{}";
            var ret = await Deserialize(request);
            ret.IsSuccessful.ShouldBeTrue();
            ret.Single.Query.ShouldBeNull();
            ret.Single.OperationName.ShouldBeNull();
            ret.Single.Inputs.ShouldBeNull();
            ret.Single.Extensions.ShouldBeNull();
        }

        [Fact]
        public async Task Decodes_BigInteger()
        {
            var request = @"{""variables"":{""a"":1234567890123456789012345678901234567890}}";
            var ret = await Deserialize(request);
            var bi = BigInteger.Parse("1234567890123456789012345678901234567890");
            ret.Single.Inputs["a"].ShouldBeOfType<BigInteger>().ShouldBe(bi);
        }

        [Fact]
        public async Task Dates_Should_Parse_As_Text()
        {
            var ret = await Deserialize(@"{""variables"":{""date"":""2015-12-22T10:10:10+03:00""}}");
            ret.Single.Inputs["date"].ShouldBeOfType<string>().ShouldBe("2015-12-22T10:10:10+03:00");
        }

        [Fact]
        public async Task Extensions_Null_When_Not_Provided()
        {
            var ret = await Deserialize(@"{""variables"":{""date"":""2015-12-22T10:10:10+03:00""}}");
            ret.Single.Extensions.ShouldBeNull();
        }

        [Fact]
        public async Task Name_Matching_Is_Case_Sensitive()
        {
            var ret = await Deserialize(@"{""VARIABLES"":{""date"":""2015-12-22T10:10:10+03:00""}}");
            ret.Single.Inputs.ShouldBeNull();
        }

        [Fact]
        public async Task Decodes_Multiple_Queries()
        {
            var ret = await Deserialize(@"[{""query"":""abc""},{""query"":""def""}]");
            ret.Batch.Length.ShouldBe(2);
            ret.Batch[0].Query.ShouldBe("abc");
            ret.Batch[1].Query.ShouldBe("def");
        }

        private async Task<GraphQLRequestDeserializationResult> Deserialize(string jsonText)
        {
            var jsonStream = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(jsonText));
            var httpRequest = new TestHttpRequest
            {
                Body = jsonStream,
            };
            var deserializer = new SystemTextJson.GraphQLRequestDeserializer(_ => { });
            return await deserializer.DeserializeFromJsonBodyAsync(httpRequest);
        }
    }
}
