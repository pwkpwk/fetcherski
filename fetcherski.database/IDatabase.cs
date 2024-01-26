using fetcherski.client;

namespace fetcherski.database;

public interface IDatabase
{
    Task<QueryResult<Client.Item>> QueryLooseItemsAsync(int pageSize, Order order, CancellationToken cancellation);
    
    Task<QueryResult<Client.Item>> QueryPackItemsAsync(int pageSize, Order order, CancellationToken cancellation);

    Task<QueryResult<Client.Item>> ContinueAsync(string continuationToken, CancellationToken cancellation);

    enum Order
    {
        Ascending,
        Descending
    }
}