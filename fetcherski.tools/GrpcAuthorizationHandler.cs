using CommunityToolkit.HighPerformance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace fetcherski.tools;

/// <summary>
/// Implementation of <see cref="IAuthorizationHandler"/> registered in the dependency injection container.
/// </summary>
/// <param name="httpContextAccessor">Optional helper, supplied by dependency injection, that obtains
/// the HTTP context of the authorising request. The same context is available from the
/// <see cref="AuthorizationHandlerContext.Resource"/> property of the context parameter of the HandleRequirementAsync
/// method.</param>
/// <param name="logger">Logger supplied by dependency injection.</param>
public class GrpcAuthorizationHandler(
    IHttpContextAccessor contextAccessor,
    ILogger<GrpcAuthorizationHandler> logger) : IAuthorizationHandler
{
    private enum LogEvent
    {
        Authorized,
        TagNotRequired,
        TokenNotRequired,
        NoEndpoint,
        Failed,
        UnavailableContext,
        Unauthorized,
        NoProvider,
        NoToken,
        AuthorizationHeader,
    }

    private static EventId MakeEvent(LogEvent eventId) => new((int)eventId, nameof(IAuthorizationHandler.HandleAsync));
    private static readonly EventId AuthorizedEventId = MakeEvent(LogEvent.Authorized);
    private static readonly EventId TagNotRequiredEventId = MakeEvent(LogEvent.TagNotRequired);
    private static readonly EventId TokenNotRequiredEventId = MakeEvent(LogEvent.TokenNotRequired);
    private static readonly EventId NoEndpointEventId = MakeEvent(LogEvent.NoEndpoint);
    private static readonly EventId FailedEventId = MakeEvent(LogEvent.Failed);
    private static readonly EventId UnavailableContextEventId = MakeEvent(LogEvent.UnavailableContext);
    private static readonly EventId UnauthorizedEventId = MakeEvent(LogEvent.Unauthorized);
    private static readonly EventId NoProviderEventId = MakeEvent(LogEvent.NoProvider);
    private static readonly EventId NoTokenEventId = MakeEvent(LogEvent.NoToken);
    private static readonly EventId AuthorizationHeaderEventId = MakeEvent(LogEvent.AuthorizationHeader);

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
        var authorization = httpContext.RequestServices.GetService<IAuthorization>();

        if (authorization is null)
        {
            logger.LogError(NoProviderEventId, "No authorization provider for {endpoint}", endpoint.DisplayName);
            context.Fail(new AuthorizationFailureReason(this, "Authorization provider is not available"));
            return;
        }

        // Check requirements
        foreach (var requirement in context.Requirements)
        {
            if (requirement is GrpcTagRequirement grpcTagRequirement)
            {
                await HandleTagRequirementAsync(context, httpContext, endpoint, authorization, grpcTagRequirement);
            }
            else if (requirement is GrpcKerbungleRequirement grpcKerbungleRequirement)
            {
                await HandleKerbungleRequirementAsync(context, httpContext, authorization, grpcKerbungleRequirement);
            }

            if (context.HasFailed)
            {
                break;
            }
        }
    }

    private async Task HandleTagRequirementAsync(
        AuthorizationHandlerContext context,
        HttpContext httpContext,
        Endpoint endpoint,
        IAuthorization authorization,
        GrpcTagRequirement requirement)
    {
        if (!requirement.TagRequired)
        {
            logger.LogTrace(TagNotRequiredEventId, "Tag is not required");
            context.Succeed(requirement);
            return;
        }

        var grpcTag = endpoint.Metadata.GetMetadata<GrpcTagAttribute>();

        if (grpcTag is null)
        {
            logger.LogInformation(FailedEventId, "Untagged method {method}", endpoint.DisplayName);
            context.Fail(new AuthorizationFailureReason(this, "Untagged method"));
            return;
        }

        bool authorized = await authorization.AuthorizeActionAsync(grpcTag.Action, httpContext.RequestAborted)
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

    private async Task HandleKerbungleRequirementAsync(
        AuthorizationHandlerContext context,
        HttpContext httpContext,
        IAuthorization authorization,
        GrpcKerbungleRequirement requirement)
    {
        if (!requirement.KerbungleTokenRequired)
        {
            logger.LogTrace(TokenNotRequiredEventId, "Token is not required");
            context.Succeed(requirement);
            return;
        }

        foreach (var header in httpContext.Request.Headers.Authorization)
        {
            logger.LogDebug(AuthorizationHeaderEventId, "Authorization header: '{header}'", header);
            string? token = RetrieveAuthorizationToken(header);

            if (token is not null && await authorization.AuthorizeTokenAsync(token, httpContext.RequestAborted))
            {
                context.Succeed(requirement);
                return;
            }
        }

        logger.LogError(NoTokenEventId, "No valid token");
        context.Fail(new AuthorizationFailureReason(this, "No valid token"));
    }

    private string? RetrieveAuthorizationToken(string? authorizationHeader)
    {
        if (authorizationHeader is null)
        {
            return null;
        }

        var kerbungle = "Kerbungle".AsSpan();
        int i = 0;

        foreach (var token in authorizationHeader.Tokenize(' '))
        {
            switch (i++)
            {
                case 0:
                    if (!token.SequenceEqual(kerbungle))
                    {
                        return null;
                    }

                    break;

                case 1:
                    return token.ToString();
                    break;

                default:
                    return null;
            }
        }

        return null;
    }
}