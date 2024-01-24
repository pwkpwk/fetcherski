namespace fetcherski.database;

public interface IDatabase
{
    Task<QueryResult<string>> StartQuery(int pageSize, Order order, CancellationToken cancellation);

    Task<QueryResult<string>> ContinueQuery(string continuationToken, CancellationToken cancellation);

    enum Order
    {
        Ascending,
        Descending
    }
}