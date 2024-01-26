using fetcherski.client;

await foreach (var array in new Client(new Uri("https://localhost:7118/")).Query(2, false))
{
    foreach (var item in array)
    {
        Console.WriteLine(item);
    } 
}