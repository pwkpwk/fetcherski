using fetcherski.database.Configuration;
using Microsoft.Extensions.Options;

namespace fetcherski.database;

public sealed class CockroachDatabase(IOptionsSnapshot<CockroachConfig> options) : IDatabase, IDisposable
{
    private readonly CancellationTokenSource _cts = new();

    async Task<QueryResult<string>> IDatabase.StartQuery(CancellationToken cancellation)
    {
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation, _cts.Token);
        return new QueryResult<string>("continuation-token", [options.Value.CockroachKey]);
    }

    async Task<QueryResult<string>> IDatabase.ContinueQuery(string continuationToken, CancellationToken cancellation)
    {
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation, _cts.Token);
        return new QueryResult<string>(null, null);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}