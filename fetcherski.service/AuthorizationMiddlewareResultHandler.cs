using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace fetcherski.service;

public class AuthorizationMiddlewareResultHandler(
    ILogger<AuthorizationMiddlewareResultHandler> logger) : IAuthorizationMiddlewareResultHandler
{
    private static readonly EventId AuthorizedEventId = new(1, nameof(IAuthorizationMiddlewareResultHandler.HandleAsync));
    private static readonly EventId UnauthorisedEventId = new(2, nameof(IAuthorizationMiddlewareResultHandler.HandleAsync));
    private static readonly EventId RequirementEventId = new(3, nameof(IAuthorizationMiddlewareResultHandler.HandleAsync));

    Task IAuthorizationMiddlewareResultHandler.HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (!authorizeResult.Succeeded)
        {
            foreach (var requirement in policy.Requirements)
            {
                logger.LogError(RequirementEventId, "Unsatisfied policy requirement {requirement}", requirement);
            }

            logger.LogError(UnauthorisedEventId, "Unauthorized {endpoint}", context.GetEndpoint());
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        logger.LogTrace(AuthorizedEventId, "Authorized {endpoint}", context.GetEndpoint());
        return next(context);
    }
}