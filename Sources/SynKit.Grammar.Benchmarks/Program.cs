using BenchmarkDotNet.Running;

namespace SynKit.Grammar.Benchmarks;

public class Program
{
    static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run(typeof(Program).Assembly);
    }
}
