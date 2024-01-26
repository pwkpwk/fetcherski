using fetcherski.client;

namespace fetcherski.database;

public interface IDatabase
{
    Task<QueryResult<Client.Item>> StartQuery(int pageSize, Order order, CancellationToken cancellation);

    Task<QueryResult<Client.Item>> ContinueQuery(string continuationToken, CancellationToken cancellation);

    enum Order
    {
        Ascending,
        Descending
    }
}