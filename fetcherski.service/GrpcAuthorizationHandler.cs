using Microsoft.AspNetCore.Authorization;

namespace fetcherski.service;

public class GrpcAuthorizationHandler(
    IHttpContextAccessor httpContextAccessor,
    ILogger<GrpcAuthorizationHandler> logger) : AuthorizationHandler<GrpcAuthorization>
{
    private static readonly EventId AuthorizedEventId = new(1, nameof(HandleRequirementAsync));
    private static readonly EventId NoEndpointEventId = new(2, nameof(HandleRequirementAsync));
    private static readonly EventId FailedEventId = new(3, nameof(HandleRequirementAsync));
    private static readonly EventId TraceEventId = new(4, nameof(HandleRequirementAsync));

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, GrpcAuthorization requirement)
    {
        if (context.Resource is DefaultHttpContext httpContext)
        {
            var endpoint = httpContext.GetEndpoint();

            if (endpoint is null)
            {
                logger.LogInformation(NoEndpointEventId, "No endpoint");
                context.Fail();
                return Task.CompletedTask;
            }

            foreach (object item in endpoint.Metadata)
            {
                if (item is GrpcTagAttribute tag)
                {
                    logger.LogInformation(AuthorizedEventId, "Authorized '{tag}'", tag.Name);
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }
            }
        }
        
        logger.LogInformation(FailedEventId, "Untagged method");
        context.Fail();
        return Task.CompletedTask;
    }
}