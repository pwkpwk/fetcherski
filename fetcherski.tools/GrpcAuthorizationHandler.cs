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
/// <remarks>
/// <para>The <see cref="AuthorizationHandler{GrpcTagRequirement}"/> base class is a convenience helper for situations when
/// the application uses only one custom requirement class. <see cref="GrpcAuthorizationHandler"/> is registered
/// in the dependency injection container as the only implementation of <see cref="IAuthorizationHandler"/> and all
/// policy-based authorization goes through the class.</para>
/// <para>An alternative to the single <see cref="IAuthorizationHandler"/> innn the dependency injection container
/// is the <see cref="IAuthorizationHandlerProvider"/> object that can implement custom logic for selection
/// of a collection of authorization handlers for each request.</para> 
/// </remarks>
public class GrpcAuthorizationHandler(
    IHttpContextAccessor contextAccessor,
    ILogger<GrpcAuthorizationHandler> logger) : AuthorizationHandler<GrpcTagRequirement>
{
    private static readonly EventId AuthorizedEventId = new(1, nameof(HandleRequirementAsync));
    private static readonly EventId TagNotRequiredEventId = new(2, nameof(HandleRequirementAsync));
    private static readonly EventId NoEndpointEventId = new(3, nameof(HandleRequirementAsync));
    private static readonly EventId FailedEventId = new(4, nameof(HandleRequirementAsync));
    private static readonly EventId UnavailableContextEventId = new(5, nameof(HandleRequirementAsync));
    private static readonly EventId UnauthorizedEventId = new(6, nameof(HandleRequirementAsync));
    private static readonly EventId NoProviderEventId = new(7, nameof(HandleRequirementAsync));

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        GrpcTagRequirement requirement)
    {
        if (!requirement.TagRequired)
        {
            logger.LogTrace(TagNotRequiredEventId, "Tag is not required");
            context.Succeed(requirement);
            return;
        }

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

        var grpcTag = endpoint.Metadata.GetMetadata<GrpcTagAttribute>();

        if (grpcTag is null)
        {
            logger.LogInformation(FailedEventId, "Untagged method {method}", endpoint.DisplayName);
            context.Fail(new AuthorizationFailureReason(this, "Untagged method"));
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

        bool authorized = await authorization.AuthorizeAsync(grpcTag.Name, httpContext.RequestAborted)
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
}