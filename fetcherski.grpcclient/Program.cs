// See https://aka.ms/new-console-template for more information

using Grpc.Core;
using Grpc.Net.Client;

if (args.Length != 1)
{
    Console.Error.WriteLine("Use: fetcherski.grpcclient <service URI>");    
    return -1;
}

var channel = GrpcChannel.ForAddress(args[0]);
var client = new Fetcherski.FetcherskiClient(channel);

var reply = client.FetchAsync(
    new FetchRequest { Id = 1234 },
    new CallOptions(null, null, CancellationToken.None));

var response = await reply.ResponseAsync;

Console.WriteLine(response);
return 0;
