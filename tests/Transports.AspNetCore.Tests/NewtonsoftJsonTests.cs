using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Execution;
using GraphQL.NewtonsoftJson;
using GraphQL.Server.Transports.Subscriptions.Abstractions;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Shouldly;
using Xunit;

namespace GraphQL.Server.Transports.AspNetCore.Tests
{
    public partial class NewtonsoftJsonTests
    {
        [Fact]
        public async Task Dates_Should_Parse_As_Text()
        {
            var jsonText = @"{""variables"":{""date"":""2015-12-22T10:10:10+03:00""}}";
            var jsonStream = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(jsonText));
            var httpRequest = new TestHttpRequest
            {
                Body = jsonStream
            };
            var deserializer = new NewtonsoftJson.GraphQLRequestDeserializer(_ => { });
            var ret = await deserializer.DeserializeFromJsonBodyAsync(httpRequest);
            ret.Batch.ShouldBeNull();
            ret.Single.ShouldNotBeNull();
            ret.Single.Inputs.ShouldNotBeNull();
            ret.Single.Inputs["date"].ShouldBeOfType<string>().ShouldBe("2015-12-22T10:10:10+03:00");
        }
    }
}
