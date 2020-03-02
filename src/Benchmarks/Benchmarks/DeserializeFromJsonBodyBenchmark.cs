using System.IO;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using GraphQL.Server.Transports.AspNetCore;
using GraphQL.SystemTextJson;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using NsjDeserializer = GraphQL.Server.Transports.AspNetCore.NewtonsoftJson.GraphQLRequestDeserializer;
using StjDeserializer = GraphQL.Server.Transports.AspNetCore.SystemTextJson.GraphQLRequestDeserializer;

namespace GraphQL.Server.Benchmarks
{
    [MemoryDiagnoser]
    [RPlotExporter, CsvMeasurementsExporter]
    public class DeserializeFromJsonBodyBenchmark
    {
        private NsjDeserializer _nsjDeserializer;
        private StjDeserializer _stjDeserializer;
        private HttpRequest _httpRequest;
        private HttpRequest _httpRequest2;

        private const string SHORT_JSON = @"{
  ""key0"": null,
  ""key1"": true,
  ""key2"": 1.2,
  ""key3"": 10,
  ""dict"": { },
  ""key4"": ""value"",
  ""arr"": [1,2,3],
  ""key5"": {
    ""inner1"": null,
    ""inner2"": 14
  }
}";

        [GlobalSetup]
        public void GlobalSetup()
        {
            _nsjDeserializer = new NsjDeserializer(s => { });
            _stjDeserializer = new StjDeserializer(s => { });

            var gqlRequest = new GraphQLRequest { Query = SchemaIntrospection.IntrospectionQuery };
            var gqlRequestJson = Serializer.ToJson(gqlRequest);
            _httpRequest = GetHttpRequestFor(gqlRequestJson);

            gqlRequest.OperationName = "someOperationName";
            gqlRequest.Inputs = SHORT_JSON.ToInputs();
            var gqlRequestJson2 = Serializer.ToJson(gqlRequest);
            _httpRequest2 = GetHttpRequestFor(gqlRequestJson2);
        }

        private static HttpRequest GetHttpRequestFor(string gqlRequestJson)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Features.Set<IRequestBodyPipeFeature>(new RequestBodyPipeFeature(httpContext));
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(gqlRequestJson));
            return httpContext.Request;
        }

        [IterationSetup]
        public void IterationSetup()
        {
            // Reset stream positions
            _httpRequest.Body.Position = 0;
            _httpRequest2.Body.Position = 0;
        }

        [Benchmark(Baseline = true)]
        public Task<GraphQLRequestDeserializationResult> NewtonsoftJson() => _nsjDeserializer.DeserializeFromJsonBodyAsync(_httpRequest);

        [Benchmark]
        public Task<GraphQLRequestDeserializationResult> SystemTextJson() => _stjDeserializer.DeserializeFromJsonBodyAsync(_httpRequest);

        [Benchmark]
        public Task<GraphQLRequestDeserializationResult> NewtonsoftJson_WithOpNameAndVariables() => _nsjDeserializer.DeserializeFromJsonBodyAsync(_httpRequest2);

        [Benchmark]
        public Task<GraphQLRequestDeserializationResult> SystemTextJson_WithOpNameAndVariables() => _stjDeserializer.DeserializeFromJsonBodyAsync(_httpRequest2);
    }
}
