// See https://aka.ms/new-console-template for more information

using fetcherski.client;

var client = new Client(new Uri("https://localhost:7118/"));

await foreach (var array in client.Query(50))
{
    foreach (var str in array)
    {
        Console.WriteLine(str);
    } 
}