using BenchmarkDotNet.Attributes;
using fetcherski.tools;

namespace fetcherski.benchmarks.tools;

[MemoryDiagnoser, ReturnValueValidator(failOnError: true)]
public class UnfoldAsyncSequence
{
    [Params(100, 1000)] public int SequenceLength;

    [Benchmark(Baseline = true, Description = "Return a completed Task")]
    public async Task<int> ReturnTask()
    {
        int count = 0;
        
        await foreach (var _ in AsyncSequences.Unfold(
                           (state, _) => Task.FromResult((state + 1, state + 1, state < SequenceLength)), 0))
        {
            ++count;
        }

        return count;
    }

    [Benchmark(Description = "Return a completed ValueTask")]
    public async Task<int> ReturnValueTask()
    {
        int count = 0;
        
        await foreach (var _ in AsyncSequences.ValueUnfold(
                           (state, _) => ValueTask.FromResult((state + 1, state + 1, state < SequenceLength)), 0))
        {
            ++count;
        }

        return count;
    }

    [Benchmark(Description = "Compiler writes Task")]
    public async Task<int> AsyncKeyword()
    {
        int count = 0;
        
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        await foreach (var _ in AsyncSequences.Unfold(
                           async (state, _) => (state + 1, state + 1, state < SequenceLength), 0))
        {
            ++count;
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        return count;
    }

    [Benchmark(Description = "Compiler writes ValueTask")]
    public async Task<int> ValueAsyncKeyword()
    {
        int count = 0;
        
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        await foreach (var _ in AsyncSequences.ValueUnfold(
                           async (state, _) => (state + 1, state + 1, state < SequenceLength), 0))
        {
            ++count;
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        return count;
    }
}