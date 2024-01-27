using fetcherski.tools;

namespace fetcherski.test.tools;

[TestFixture(TestOf = typeof(AsyncSequences), Category = "fetcherski.tools")]
public class AsyncSequencesTests
{
    [Test]
    public async Task Unfold_CorrectSequence()
    {
        var sequence = AsyncSequences.Unfold((state, _) => Task.FromResult((state + 1, state + 1, state < 10)), 0);
        var data = new List<int>();

        await foreach (var item in sequence)
        {
            data.Add(item);
        }

        Assert.That(data, Is.EquivalentTo(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }));
    }
}