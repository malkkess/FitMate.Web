using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace FitMate.Web.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class EnsureUserMatchAttribute : Attribute, IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.ActionArguments.TryGetValue("userId", out var userIdObj) &&
                userIdObj is int routeUserId)
            {
                var claim = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (claim is null || !int.TryParse(claim, out var tokenUserId) || tokenUserId != routeUserId)
                {
                    context.Result = new ForbidResult();
                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}
