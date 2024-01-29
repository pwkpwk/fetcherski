using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using fetcherski.tools;

namespace fetcherski.benchmarks.tools;

[MemoryDiagnoser]
public class UnfoldSheerSpeed
{
    [Params(5000, 50000)] public int SequenceLength;

    [Benchmark(Baseline = true, Description = "Sheer | synchronous loop")]
    public int SynchronousLoop()
    {
        int lastObserved = 0;

        for (int n = 1; n <= SequenceLength; ++n)
        {
            lastObserved = n;
        }

        return lastObserved;
    }

    [Benchmark(Description = "Sheer | yield return")]
    public int YieldLoop() => YieldLoopAsync().Result;

    private async Task<int> YieldLoopAsync()
    {
        int lastObserved = 0;

        await foreach (var n in Loop(CancellationToken.None))
        {
            lastObserved = n;
        }

        return lastObserved;
    }

    [Benchmark(Description = "Sheer | AsyncSequences.Unfold")]
    public int Unfold() => UnfoldAsync().Result;

    private async Task<int> UnfoldAsync()
    {
        int lastObserved = 0;

        await foreach (var n in AsyncSequences.Unfold(Fold, 0))
        {
            lastObserved = n;
        }

        return lastObserved;
    }

    private Task<(int, int, bool)> Fold(int state, CancellationToken _)
    {
        return Task.FromResult((state + 1, state + 1, state < SequenceLength));
    }

    private async IAsyncEnumerable<int> Loop([EnumeratorCancellation] CancellationToken ct)
    {
        bool hasData;
        int state = 0;

        do
        {
            (state, var value, hasData) = await Fold(state, ct);
            yield return value;
        } while (hasData);
    }
}