using fetcherski.client;

await foreach (var array in new Client(new Uri("https://localhost:7118/")).Query(2, true))
{
    foreach (var str in array)
    {
        Console.WriteLine(str);
    } 
}