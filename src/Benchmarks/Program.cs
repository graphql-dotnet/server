using BenchmarkDotNet.Running;

namespace GraphQL.Server.Benchmarks;

public class Program
{
    // Call without args to just run the body deserializer benchmark
    // Call with an int arg to toggle different benchmarks
    public static void Main(string[] args)
    {
        if (!args.Any() || !int.TryParse(args[0], out int benchmarkIndex))
        {
            benchmarkIndex = 0;
        }

        _ = benchmarkIndex switch
        {
            1 => BenchmarkRunner.Run<DeserializeInputsFromJsonBenchmark>(),
            _ => BenchmarkRunner.Run<DeserializeFromJsonBodyBenchmark>(),
        };
    }
}
