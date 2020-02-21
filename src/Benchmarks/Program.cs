using BenchmarkDotNet.Running;

namespace GraphQL.Server.Benchmarks
{
    class Program
    {
        private static void Main(string[] args)
        {
            BenchmarkRunner.Run<DeserializeFromJsonBodyBenchmark>();
            //BenchmarkRunner.Run<DeserializeInputsFromJsonBenchmark>();
        }
    }
}
