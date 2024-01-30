using System.Reflection;
using BenchmarkDotNet.Running;
using fetcherski.database.Configuration;
using Microsoft.Extensions.Configuration;

namespace fetcherski.benchmarks;

static class Program
{
    static void Main(string[] args)
    {
        var config = LoadDatabaseConfig();
        
        Console.Out.WriteLine($"Database={config.Database}, Schema={config.Schema}, User={config.User}, Host={config.Host}");
        
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

    public static CockroachConfig LoadDatabaseConfig()
    {
        var root = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables("fetcherski.")
            .Build();

        return root.GetSection("CockroachDB").Get<CockroachConfig>()!;
    }
}