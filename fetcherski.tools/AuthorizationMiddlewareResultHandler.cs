using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace fetcherski.tools;

/// <summary>
/// Handler of results of policy-based authorization of a received request.
/// </summary>
/// <param name="logger">Logger supplied by dependency injection.</param>
/// <remarks>
/// <para>The authorization infrastructure by itself does not fail incoming requests, it only records results of work
/// of its parts. After the authorization has finished, a registered handler is called to apply authorization
/// results to the request.</para>
/// <para>This class fails requests with the 401 Unauthorized HTTP response.</para>
/// </remarks>
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

        logger.LogTrace(AuthorizedEventId, "Allow authorized request {endpoint}", context.GetEndpoint());
        return next(context);
    }
}