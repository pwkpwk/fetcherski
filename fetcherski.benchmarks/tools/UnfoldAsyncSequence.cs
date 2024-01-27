using BenchmarkDotNet.Attributes;
using fetcherski.tools;

namespace fetcherski.benchmarks.tools;

[MemoryDiagnoser]
public class UnfoldAsyncSequence
{
    [Params(100, 1000)] public int SequenceLength;

    [Benchmark(Description = "Return a completed task")]
    public async Task ReturnTask()
    {
        var sequence =
            AsyncSequences.Unfold((state, _) => Task.FromResult((state + 1, state + 1, state < SequenceLength)), 0);

        await foreach (var _ in sequence)
        {
            // Do nothing            
        }
    }

    [Benchmark(Description = "Compiler writes async method")]
    public async Task AsyncKeyword()
    {
        var sequence = AsyncSequences.Unfold(async (state, _) => (state + 1, state + 1, state < SequenceLength), 0);

        await foreach (var _ in sequence)
        {
            // Do nothing            
        }
    }
}