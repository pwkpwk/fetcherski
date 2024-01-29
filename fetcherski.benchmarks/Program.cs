using System.Reflection;
using BenchmarkDotNet.Running;

namespace fetcherski.benchmarks;

static class Program
{
    static void Main(string[] args)
    {
        if (args.Length > 0)
        {
            var assembly = Assembly.GetEntryAssembly();
            Console.Out.WriteLine($"{assembly.GetName().Name}: {string.Join(" ", args)}");
            BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly()).Run(args);
        }
        else
        {
            BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly()).RunAll();
        }
    }
}