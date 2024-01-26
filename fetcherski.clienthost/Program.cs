using fetcherski.client;

await foreach (var array in new Client(new Uri("https://localhost:7118/")).QueryLooseItemsAsync(2, true))
{
    foreach (var item in array)
    {
        Console.WriteLine(item);
    } 
}