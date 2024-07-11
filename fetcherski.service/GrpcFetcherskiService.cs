using fetcherski.tools;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace fetcherski.service;

/// <summary>
/// API controller that implements the Fetcherski gRPC service contract defined in the fetcherski.grpc project.
/// </summary>
/// <param name="logger">Logger supplied by dependency injection.</param>
/// <remarks><para>ASP.Net will apply authorization policy "GrpcTagRequirement" to all incoming gRPC requests,
/// as is requested by the <see cref="AuthorizeAttribute"/> attribute. The policy is established in the
/// AddAuthorization call i the service startup file, Program.cs.</para>
/// <para>ASP.Net combines all requirements from all policies into one collection that it passes to one call
/// <see cref="IAuthorizationHandler.HandleAsync"/></para>.
/// <para>After all authorization handlers have finished, one
/// authorization middleware request handler renders the result by letting the request through the rest of the
/// middleware chain or by failing it.</para>
/// <para>It is on the developer to not apply contradicting requirements to gRPC and HTTP API controllers.</para></remarks>
/// <seealso cref="AuthorizationMiddlewareResultHandler"/>
[Authorize(nameof(ActionNameRequirement))]
[Authorize(nameof(KerbungleRequirement))]
public class GrpcFetcherskiService(ILogger<GrpcFetcherskiService> logger) : Fetcherski.FetcherskiBase
{
    private static readonly EventId FetchEventId = new(1, nameof(Fetch));

    /// <summary>
    /// Implementation of the Fetcherski.Fetch gRPC contract.
    /// </summary>
    /// <remarks>
    /// The <see cref="ActionNameAttribute"/> attribute is checked by <see cref="FetcherskiAuthorizationHandler"/> if the
    /// <see cref="ActionNameRequirement"/> requirement is present in the collection of requirements passed to
    /// <see cref="IAuthorizationHandler.HandleAsync"/>, that examines metadata of the .Net method bound to the
    /// HTTP endpoint that processes the received gRPC request.
    /// </remarks>
    [ActionName("Wormwood")]
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