using fetcherski.tools;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace fetcherski.service;

/// <summary>
/// Implementation of the gRPC service Fetcherski declared in the fetcherski.grpc project.
/// </summary>
/// <param name="logger">Logger supplied by dependency injection.</param>
/// <remarks>ASP.Net will apply authorization policy "GrpcTagRequirement" to all incoming gRPC requests,
/// as is requested by the <see cref="AuthorizeAttribute"/> attribute. The policy is established in the
/// AddAuthorization call i the service startup file, Program.cs.</remarks>
[Authorize(nameof(GrpcTagRequirement))]
public class FetcherskiService(ILogger<FetcherskiService> logger) : Fetcherski.FetcherskiBase
{
    private static readonly EventId FetchEventId = new(1, nameof(Fetch));

    [GrpcTag("Wormwood")]
    public override Task<FetchReply> Fetch(FetchRequest request, ServerCallContext context)
    {
        logger.LogInformation(FetchEventId, "id={requestId}", request.Id);
        
        return Task.FromResult(new FetchReply
        {
            Id = request.Id,
            Name = $"{request.Id}: Name",
            Location = $"{request.Id}: Location",
            Age = Random.Shared.Next()
        });
    }

    public override Task<FlipReply> Flip(FlipRequest request, ServerCallContext context)
    {
        return Task.FromResult(new FlipReply{ Flipped = Random.Shared.Next(2) == 1});
    }
}