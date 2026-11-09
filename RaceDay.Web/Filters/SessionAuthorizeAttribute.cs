using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using RaceDay.Web.Helpers;

namespace RaceDay.Web.Filters;

/// <summary>
/// Protects a controller or action. Not logged in -> login page.
/// Wrong role -> back to the home page with a message.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string? role;

    public SessionAuthorizeAttribute(string? role = null) { this.role = role; }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;

        if (session.GetInt32(SessionKeys.UserId) == null)
        {
            string returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        if (role != null && session.GetString(SessionKeys.Role) != role)
        {
            var tempDataFactory = context.HttpContext.RequestServices.GetRequiredService<ITempDataDictionaryFactory>();
            tempDataFactory.GetTempData(context.HttpContext)["Error"] = "You do not have access to that page.";

            context.Result = new RedirectToActionResult("Index", "Home", null);
        }
    }
}
