using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace fetcherski.tools;

public class FetcherskiAuthorizationAttribute(string? Name = null) : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var authorization = (IAuthorization)context.HttpContext.RequestServices.GetService(typeof(IAuthorization))!;
        string actionName = Name ?? context.ActionDescriptor.RouteValues["action"]!; 

        if (! await authorization.AuthorizeAsync(actionName, context.HttpContext.RequestAborted))
        {
            context.Result = new UnauthorizedResult();
            return;
        }
        
        // Base implementation routes the action to the selected controller
        await base.OnActionExecutionAsync(context, next);
    }
}