namespace fetcherski.tools;

public static class AsyncSequences
{
    public static IAsyncEnumerable<T> Unfurl<T>(this IAsyncEnumerable<IEnumerable<T>> source) =>
        new UnfurlingEnumerable<T>(source);

    public static IAsyncEnumerable<T> MergeSort<T>(IComparer<T> order, params IAsyncEnumerable<T>[] sources) =>
        new MergeSoringEnumerable<T>(order, sources);

    private sealed class UnfurlingEnumerable<T>(IAsyncEnumerable<IEnumerable<T>> source) : IAsyncEnumerable<T>
    {
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellation) =>
            new Enumerator(source.GetAsyncEnumerator(cancellation));

        private sealed class Enumerator(IAsyncEnumerator<IEnumerable<T>> source) : IAsyncEnumerator<T>
        {
            private IEnumerator<T>? _page = null;
            private bool _exhausted = false;

            ValueTask IAsyncDisposable.DisposeAsync()
            {
                _exhausted = true;
                _page?.Dispose();
                return source.DisposeAsync();
            }

            async ValueTask<bool> IAsyncEnumerator<T>.MoveNextAsync()
            {
                while (!_exhausted)
                {
                    if (_page is null)
                    {
                        if (!await source.MoveNextAsync())
                        {
                            _exhausted = true;
                            return false;
                        }

                        _page = source.Current.GetEnumerator();
                    }

                    if (_page.MoveNext())
                    {
                        return true;
                    }

                    _page = null;
                }

                return false;
            }

            T IAsyncEnumerator<T>.Current => _page is not null ? _page.Current : default;
        }
    }

    private sealed class MergeSoringEnumerable<T>(IComparer<T> order, IEnumerable<IAsyncEnumerable<T>> sources)
        : IAsyncEnumerable<T>
    {
        IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellation) =>
            new Enumerator(order, sources, cancellation);

        private sealed class Enumerator(
            IComparer<T> order,
            IEnumerable<IAsyncEnumerable<T>> sources,
            CancellationToken cancellation) : IAsyncEnumerator<T>
        {
            private readonly CancellationTokenSource _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            private PriorityQueue<IAsyncEnumerator<T>, T>? _queue = null;
            private T _current = default;
            private bool _exhausted = false;

            async ValueTask IAsyncDisposable.DisposeAsync()
            {
                _exhausted = true;
                await _cts.CancelAsync();
                foreach (var item in _queue.UnorderedItems)
                {
                    await item.Element.DisposeAsync();
                }
                _queue.Clear();
                _cts.Dispose();
            }

            async ValueTask<bool> IAsyncEnumerator<T>.MoveNextAsync()
            {
                while (!_exhausted)
                {
                    if (_queue is null)
                    {
                        _queue = await MakeQueue(order, sources, _cts.Token);
                    }

                    if (_queue.TryDequeue(out var source, out var value))
                    {
                        _current = value;
                        if (await source.MoveNextAsync())
                        {
                            _queue.Enqueue(source, source.Current);
                        }
                        else
                        {
                            await source.DisposeAsync();
                        }
                        return true;
                    }
                    else
                    {
                        _exhausted = true;
                    }
                }

                return false;
            }

            public T Current => _current;

            private static async ValueTask<PriorityQueue<IAsyncEnumerator<T>, T>> MakeQueue(
                IComparer<T> order,
                IEnumerable<IAsyncEnumerable<T>> sources,
                CancellationToken cancellation)
            {
                var queue = new PriorityQueue<IAsyncEnumerator<T>, T>(order);

                foreach (var source in sources)
                {
                    var enumerator = source.GetAsyncEnumerator(cancellation);

                    if (await enumerator.MoveNextAsync())
                    {
                        queue.Enqueue(enumerator, enumerator.Current);                        
                    }
                    else
                    {
                        await enumerator.DisposeAsync();
                    }
                }

                return queue;
            }
        }
    }
}