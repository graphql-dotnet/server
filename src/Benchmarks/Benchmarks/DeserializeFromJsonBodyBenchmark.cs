using System.Text;
using BenchmarkDotNet.Attributes;
using GraphQL.Transport;
using NsjDeserializer = GraphQL.NewtonsoftJson.GraphQLSerializer;
using StjDeserializer = GraphQL.SystemTextJson.GraphQLSerializer;

namespace GraphQL.Server.Benchmarks
{
    [MemoryDiagnoser]
    [RPlotExporter, CsvMeasurementsExporter]
    public class DeserializeFromJsonBodyBenchmark
    {
        private NsjDeserializer _nsjDeserializer;
        private StjDeserializer _stjDeserializer;
        private Stream _httpRequestBody;
        private Stream _httpRequestBody2;

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
            _httpRequestBody = GetHttpRequestBodyFor(gqlRequestJson);

            gqlRequest.OperationName = "someOperationName";
            gqlRequest.Variables = new StjDeserializer().Deserialize<Inputs>(SHORT_JSON);
            var gqlRequestJson2 = Serializer.ToJson(gqlRequest);
            _httpRequestBody2 = GetHttpRequestBodyFor(gqlRequestJson2);
        }

        private static Stream GetHttpRequestBodyFor(string gqlRequestJson)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(gqlRequestJson));
        }

        [IterationSetup]
        public void IterationSetup()
        {
            // Reset stream positions
            _httpRequestBody.Position = 0;
            _httpRequestBody2.Position = 0;
        }

        [Benchmark(Baseline = true)]
        public ValueTask<GraphQLRequest[]> NewtonsoftJson() => _nsjDeserializer.ReadAsync<GraphQLRequest[]>(_httpRequestBody);

        [Benchmark]
        public ValueTask<GraphQLRequest[]> SystemTextJson() => _stjDeserializer.ReadAsync<GraphQLRequest[]>(_httpRequestBody);

        [Benchmark]
        public ValueTask<GraphQLRequest[]> NewtonsoftJson_WithOpNameAndVariables() => _nsjDeserializer.ReadAsync<GraphQLRequest[]>(_httpRequestBody2);

        [Benchmark]
        public ValueTask<GraphQLRequest[]> SystemTextJson_WithOpNameAndVariables() => _stjDeserializer.ReadAsync<GraphQLRequest[]>(_httpRequestBody2);
    }
}
