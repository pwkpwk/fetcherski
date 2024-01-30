using System.Reflection;
using BenchmarkDotNet.Running;

namespace fetcherski.benchmarks;

static class Program
{
    static void Main(string[] args)
    {
        var assembly = Assembly.GetEntryAssembly();
        
        if (args.Length > 0)
        {
            Console.Out.WriteLine($"{assembly.GetName().Name}: {string.Join(" ", args)}");
            BenchmarkSwitcher.FromAssembly(assembly).Run(args);
        }
        else
        {
            BenchmarkSwitcher.FromAssembly(assembly).RunAll();
        }
    }
}