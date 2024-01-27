using System.Reflection;
using BenchmarkDotNet.Running;

namespace fetcherski.benchmarks;

static class Program
{
    static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly()).Run(args);
    }
}