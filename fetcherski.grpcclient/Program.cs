// See https://aka.ms/new-console-template for more information

using Grpc.Core;
using Grpc.Net.Client;

if (args.Length != 1)
{
    Console.Error.WriteLine("Use: fetcherski.grpcclient <service URI>");
    return -1;
}

CallCredentials credentials = CallCredentials.FromInterceptor(async (context, metadata) =>
{
    var token = await ObtainToken(context.CancellationToken);
    metadata.Add("Authorization", $"Kerbungle {token}");
});

var channel = GrpcChannel.ForAddress(
    args[0],
    new GrpcChannelOptions
    {
        Credentials = ChannelCredentials.Create(new SslCredentials(), credentials)
    });

var client = new Fetcherski.FetcherskiClient(channel);

var fetchReply = client.FetchAsync(
    new FetchRequest { Id = 1234 },
    new CallOptions(null, null, CancellationToken.None));
Console.Out.WriteLine(await fetchReply.ResponseAsync);

try
{
    var flipReply = await client.FlipAsync(new FlipRequest(), new CallOptions(null, null, CancellationToken.None));
    Console.WriteLine($"Flipped = {flipReply.Flipped}");
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
}

return 0;

ValueTask<string> ObtainToken(CancellationToken cancellationToken)
{
    return ValueTask.FromResult("Token");
}