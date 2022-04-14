using BenchmarkDotNet.Attributes;
using NsjDeserializer = GraphQL.NewtonsoftJson.GraphQLSerializer;
using StjDeserializer = GraphQL.SystemTextJson.GraphQLSerializer;

namespace GraphQL.Server.Benchmarks;

[MemoryDiagnoser]
[RPlotExporter, CsvMeasurementsExporter]
public class DeserializeInputsFromJsonBenchmark
{
    private NsjDeserializer _nsjDeserializer;
    private StjDeserializer _stjDeserializer;
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
    }

    // Note: There's not a whole lot of value benchmarking these two implementations since their methods just
    // call directly to each's underlying core GraphQL repo's `.ToInputs` methods which are essentially benchmarked
    // over there by GraphQL.Benchmarks.DeserializationBenchmark. But this does give us the ability to benchmark any
    // other custom implementations someone else might want to contribute.

    [Benchmark(Baseline = true)]
    public Inputs NewtonsoftJson() => _nsjDeserializer.Deserialize<Inputs>(SHORT_JSON);

    [Benchmark]
    public Inputs SystemTextJson() => _stjDeserializer.Deserialize<Inputs>(SHORT_JSON);
}
