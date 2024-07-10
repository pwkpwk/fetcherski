using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace fetcherski.tools;

/// <summary>
/// Attribute that may be applied to methods of API controllers to process actions routed to the methods.
/// </summary>
/// <param name="Name">Optional name of the action that the attribute authorizes. If not specified,
/// ASP.Net derives the name from the controller method, removing "Async" if the method returns a task.</param>
public class FetcherskiAuthorizationAttribute(string? Name = null) : ActionFilterAttribute
{
    private static readonly EventId BadRequestEventId = new(400, "Authorization");
    private static readonly EventId UnauthorizedEventId = new(401, "Authorization");
    private static readonly EventId AuthorizedEventId = new(200, "Authorization");

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var authorization = GetService<IAuthorization>(context);
        var logger = GetService<ILogger<FetcherskiAuthorizationAttribute>>(context);

        string? actionName = Name ?? context.ActionDescriptor.RouteValues["action"];

        if (string.IsNullOrWhiteSpace(actionName))
        {
            logger.LogError(BadRequestEventId, "Unspecified action name");
            context.Result = new BadRequestResult();
        }
        else if (!await authorization.AuthorizeActionAsync(actionName, context.HttpContext.RequestAborted))
        {
            logger.LogError(UnauthorizedEventId, "Failed to authorize action '{actionName}'", actionName);
            context.Result = new UnauthorizedResult();
        }
        else
        {
            logger.LogTrace(AuthorizedEventId, "Authorized action '{actionName}'", actionName);
            // Base implementation routes the action to the selected controller
            await base.OnActionExecutionAsync(context, next);
        }
    }

    private static T GetService<T>(ActionExecutingContext context) where T : class =>
        (context.HttpContext.RequestServices.GetService(typeof(T)) as T)!;
}