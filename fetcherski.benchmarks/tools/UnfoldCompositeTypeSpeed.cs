using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using fetcherski.tools;

namespace fetcherski.benchmarks.tools;

[MemoryDiagnoser]
public class UnfoldCompositeTypeSpeed
{
    private static readonly Guid TestUuid = Guid.NewGuid();

    [Params(5000, 50000)] public int SequenceLength = 0;

    [Benchmark(Baseline = true, Description = "Value type | synchronous loop")]
    public int SynchronousLoop()
    {
        int lastObserved = 0;

        for (int n = 0; n < SequenceLength; ++n)
        {
            var value = new Value(n + 1, n, TestUuid);
            lastObserved = value.id;
        }

        return lastObserved;
    }

    [Benchmark(Description = "Value type | yield return")]
    public async Task<int> YieldReturnAsync()
    {
        int lastObserved = 0;

        await foreach (var v in YieldReturn(CancellationToken.None))
        {
            lastObserved = v.id;
        }

        return lastObserved;
    }

    [Benchmark(Description = "Value type | AsyncSequences.Unfold")]
    public async Task<int> UnfoldAsync()
    {
        int lastObserved = 0;

        await foreach (var v in AsyncSequences.Unfold(Fold, 0))
        {
            lastObserved = v.id;
        }

        return lastObserved;
    }

    [Benchmark(Description = "Reference type | yield return")]
    public async Task<int> RefYieldReturnAsync()
    {
        int lastObserved = 0;

        await foreach (var v in RefYieldReturn(CancellationToken.None))
        {
            lastObserved = v.id;
        }

        return lastObserved;
    }

    [Benchmark(Description = "Reference type | AsyncSequences.Unfold")]
    public async Task<int> RefUnfoldAsync()
    {
        int lastObserved = 0;

        await foreach (var v in AsyncSequences.Unfold(RefFold, 0))
        {
            lastObserved = v.id;
        }

        return lastObserved;
    }

    private record struct Value(int id, long value, Guid uuid);

    private record RefValue(int id, long value, Guid uuid);

    private Task<(int, Value, bool)> Fold(int state, CancellationToken _) =>
        Task.FromResult((state + 1, new Value(state + 1, state, TestUuid), state < SequenceLength));

    private Task<(int, RefValue, bool)> RefFold(int state, CancellationToken _) =>
        Task.FromResult((state + 1, new RefValue(state + 1, state, TestUuid), state < SequenceLength));

    private async IAsyncEnumerable<Value> YieldReturn([EnumeratorCancellation] CancellationToken ct)
    {
        bool hasData;
        int state = 0;

        do
        {
            (state, var value, hasData) = await Fold(state, ct);
            yield return value;
        } while (hasData);
    }

    private async IAsyncEnumerable<RefValue> RefYieldReturn([EnumeratorCancellation] CancellationToken ct)
    {
        bool hasData;
        int state = 0;

        do
        {
            (state, var value, hasData) = await RefFold(state, ct);
            yield return value;
        } while (hasData);
    }
}