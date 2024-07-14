using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace fetcherski.tools;

/// <summary>
/// Implementation of <see cref="IAuthorizationHandler"/> registered in the dependency injection container.
/// </summary>
/// <param name="contextAccessor">Optional helper, supplied by dependency injection, that obtains
/// the HTTP context of the authorising request. The same context is available from the
/// <see cref="AuthorizationHandlerContext.Resource"/> property of the context parameter of the HandleRequirementAsync
/// method.</param>
/// <param name="logger">Logger supplied by dependency injection.</param>
public class FetcherskiAuthorizationHandler(
    IHttpContextAccessor contextAccessor,
    ILogger<FetcherskiAuthorizationHandler> logger) : IAuthorizationHandler
{
    private enum LogEvent
    {
        Authorized,
        TagNotRequired,
        TokenNotRequired,
        NoEndpoint,
        Failed,
        UnavailableContext,
        Unauthenticated,
        Unauthorized,
        NoProvider,
        UnsatisfiedRequirement,
    }

    private static EventId MakeEvent(LogEvent eventId) => new((int)eventId, $"{nameof(IAuthorizationHandler.HandleAsync)}.{eventId}");
    private static readonly EventId AuthorizedEventId = MakeEvent(LogEvent.Authorized);
    private static readonly EventId TagNotRequiredEventId = MakeEvent(LogEvent.TagNotRequired);
    private static readonly EventId TokenNotRequiredEventId = MakeEvent(LogEvent.TokenNotRequired);
    private static readonly EventId NoEndpointEventId = MakeEvent(LogEvent.NoEndpoint);
    private static readonly EventId FailedEventId = MakeEvent(LogEvent.Failed);
    private static readonly EventId UnavailableContextEventId = MakeEvent(LogEvent.UnavailableContext);
    private static readonly EventId UnauthenticatedEventId = MakeEvent(LogEvent.Unauthenticated);
    private static readonly EventId UnauthorizedEventId = MakeEvent(LogEvent.Unauthorized);
    private static readonly EventId NoProviderEventId = MakeEvent(LogEvent.NoProvider);
    private static readonly EventId UnsatisfiedRequirementEventId = MakeEvent(LogEvent.UnsatisfiedRequirement);

    async Task IAuthorizationHandler.HandleAsync(AuthorizationHandlerContext context)
    {
        var httpContext = context.Resource as HttpContext ?? contextAccessor.HttpContext;

        if (httpContext is null)
        {
            logger.LogError(UnavailableContextEventId, "Unavailable HTTP context | {resource}", context.Resource);
            context.Fail();
            return;
        }

        // The HTTP context can also be obtained from IHttpContextAccessor injected in the constructor and added
        // to the dependency injection container by the AddHttpContextAccessor() call in the service startup
        // file, Program.cs
        var endpoint = httpContext.GetEndpoint();

        if (endpoint is null)
        {
            logger.LogInformation(
                NoEndpointEventId,
                "Endpoint is not available from authorization resource {resource}", context.Resource);
            context.Fail(new AuthorizationFailureReason(this, "No endpoint"));
            return;
        }

        // If custom authorization is needed, and it almost always is needed at this point,
        // obtain the authorization service from the DI container and call it with data
        // from the attribute applied to the class method bound to the request.
        //
        // IAuthorization is registered as a scoped object, so it cannot be injected in the constructor, but
        // if a singleton IAuthorization object is registered, there is no  need to call GetService here, and the
        // object may be injected in the constructor.
        //
        // DummyAuthorization cass, that implements IAuthorization, also implements IAsyncDisposable that shall be called
        // by the request pipeline; which means that instances obtained from GetService must neither be retained,
        // nor disposed.
        var authorization = httpContext.RequestServices.GetService<IFetcherskiAuthorization>();

        if (authorization is null)
        {
            logger.LogError(NoProviderEventId, "No authorization provider for {endpoint}", endpoint.DisplayName);
            context.Fail(new AuthorizationFailureReason(this, "Authorization provider is not available"));
            return;
        }

        // Check requirements
        foreach (var requirement in context.Requirements)
        {
            if (requirement is ActionNameRequirement grpcTagRequirement)
            {
                await ProcessActionNameRequirementAsync(context, httpContext, endpoint, authorization, grpcTagRequirement);
            }
            else if (requirement is KerbungleRequirement grpcKerbungleRequirement)
            {
                ProcessKerbungleRequirementAsync(context, endpoint, grpcKerbungleRequirement);
            }

            if (context.HasFailed)
            {
                break;
            }
        }

        if (!context.HasFailed)
        {
            foreach (var requirement in context.PendingRequirements)
            {
                string requirementType = requirement.GetType().Name;
                logger.LogError(UnsatisfiedRequirementEventId, "Unsatisfied requirement {requirement}", requirementType);
                context.Fail(new AuthorizationFailureReason(this, $"Unsatisfied requirement {requirementType}"));
            }
        }
    }

    private async Task ProcessActionNameRequirementAsync(
        AuthorizationHandlerContext context,
        HttpContext httpContext,
        Endpoint endpoint,
        IFetcherskiAuthorization fetcherskiAuthorization,
        ActionNameRequirement requirement)
    {
        if (!requirement.TagRequired)
        {
            logger.LogTrace(TagNotRequiredEventId, "Tag is not required");
            context.Succeed(requirement);
            return;
        }

        var actionNameTag = endpoint.Metadata.GetMetadata<ActionNameAttribute>();

        if (actionNameTag is null)
        {
            logger.LogError(FailedEventId, "Untagged method {method}", endpoint.DisplayName);
            context.Fail(new AuthorizationFailureReason(this, "Untagged method"));
            return;
        }

        bool authorized = await fetcherskiAuthorization.AuthorizeActionAsync(
                actionNameTag.Action,
                httpContext.RequestAborted)
            .ConfigureAwait(ConfigureAwaitOptions.None);

        if (!authorized)
        {
            logger.LogError(UnauthorizedEventId, "Unauthorized {endpoint}", endpoint.DisplayName);
            context.Fail(new AuthorizationFailureReason(this, "Unauthorized"));
            return;
        }

        logger.LogInformation(AuthorizedEventId, "Authorized {endpoint}", endpoint.DisplayName);
        context.Succeed(requirement);
    }

    private void ProcessKerbungleRequirementAsync(
        AuthorizationHandlerContext context,
        Endpoint endpoint,
        KerbungleRequirement requirement)
    {
        if (!requirement.KerbungleTokenRequired)
        {
            logger.LogTrace(TokenNotRequiredEventId, "Token is not required");
            context.Succeed(requirement);
            return;
        }

        foreach (var identity in context.User.Identities)
        {
            if (KerbungleAuthenticationOptions.Scheme.Equals(identity.AuthenticationType,
                    StringComparison.InvariantCultureIgnoreCase)
                && identity.IsAuthenticated)
            {
                logger.LogInformation(AuthorizedEventId, "Authorized '{name}' | {endpoint}",
                    identity.Name, endpoint.DisplayName);
                context.Succeed(requirement);
            }
        }

        if (!context.HasSucceeded)
        {
            logger.LogError(UnauthenticatedEventId, "Unauthenticated caller {endpoint}", endpoint);
            context.Fail(new AuthorizationFailureReason(this, "Unauthenticated caller"));
        }
    }
}