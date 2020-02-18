using BenchmarkDotNet.Running;

namespace GraphQL.Server.Benchmarks
{
    class Program
    {
        private static void Main(string[] args)
        {
            BenchmarkRunner.Run<DeserializeFromJsonBodyBenchmark>();

            //var x = new DeserializeFromJsonBodyBenchmark();
            //var result = x.NewtonsoftJson().GetAwaiter().GetResult();
            //var result = x.SystemTextJson().GetAwaiter().GetResult();
        }
    }
}
