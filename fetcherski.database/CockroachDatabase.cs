using fetcherski.database.Configuration;
using Microsoft.Extensions.Options;

namespace fetcherski.database;

public class CockroachDatabase(IOptionsSnapshot<CockroachConfig> options) : IDatabase
{
    Task<QueryResult<string>> IDatabase.StartQuery(CancellationToken cancellation)
    {
        return Task.FromResult(new QueryResult<string>("continuation-token", [options.Value.CockroachKey]));
    }

    Task<QueryResult<string>> IDatabase.ContinueQuery(string continuationToken, CancellationToken cancellation)
    {
        return Task.FromResult(new QueryResult<string>(null, null));
    }
}