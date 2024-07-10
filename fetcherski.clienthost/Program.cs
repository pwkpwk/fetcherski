using fetcherski.client;
using fetcherski.clienthost;
using fetcherski.tools;

var client = new Client(new Uri("http://localhost:5126/"));

Console.WriteLine($"UnauthorizedAsync: {await client.CallUnauthorizedAsync(CancellationToken.None)}");
Console.WriteLine($"Plop = {await client.GetPlopAsync(CancellationToken.None)}");

Console.WriteLine("FORWARD:");
await PrintItems(client, new ItemsOrder(true));
Console.WriteLine();
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
