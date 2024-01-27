using fetcherski.client;
using fetcherski.clienthost;
using fetcherski.tools;

var client = new Client(new Uri("https://localhost:7118/"));

Console.WriteLine("FORWARD:");
await PrintItems(client, new ItemsOrder(true));
Console.WriteLine("REVERSE:");
await PrintItems(client, new ItemsOrder(false));


static async Task PrintItems(Client client, ItemsOrder order)
{
    var query = AsyncSequences.MergeSort(order,
        client.QueryLooseItemsAsync(2, order.IsDescending).Unfurl(),
        client.QueryPackItemsAsync(2, order.IsDescending).Unfurl());

    await foreach (var item in query)
    {
        Console.WriteLine(item.description);
    }
}
