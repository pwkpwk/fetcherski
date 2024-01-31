using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace fetcherski.test.benchmarks.database;

[TestFixture, Category("benchmarks")]
public class TokenGenerationBenchmarkTests
{
    private IConfig? _config;

    [SetUp]
    public void SetUpTest()
    {
        _config = new Config();
    }

    [Test]
    public void TokenGeneration_ImprovedAboveBaseline()
    {
        var summary = BenchmarkRunner.Run<TokenGenerationBenchmarks>(_config);

        if (summary.Reports.Length != 2)
        {
            foreach (var error in summary.ValidationErrors)
            {
                TestContext.Error.WriteLine(error.Message);
            }

            Assert.Fail();
        }

        Assert.That(summary.Reports[0].Success, Is.True);
        Assert.That(summary.Reports[1].Success, Is.True);
        Assert.That(summary.Reports[0].BenchmarkCase.Descriptor.Baseline, Is.True);
        Assert.That(summary.Reports[1].BenchmarkCase.Descriptor.Baseline, Is.False);
        Assert.That(summary.Reports[1].ResultStatistics.Mean, Is.LessThan(summary.Reports[0].ResultStatistics.Mean));
    }

    public class Config : DebugConfig
    {
        public override IEnumerable<Job> GetJobs() => new[]
        {
            Job.Default.WithToolchain(InProcessEmitToolchain.DontLogOutput)
        };
    }
}