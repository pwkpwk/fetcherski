namespace fetcherski.database;

public interface IDatabase
{
    Task<QueryResult<string>> StartQuery(CancellationToken cancellation);

    Task<QueryResult<string>> ContinueQuery(string continuationToken, CancellationToken cancellation);
}