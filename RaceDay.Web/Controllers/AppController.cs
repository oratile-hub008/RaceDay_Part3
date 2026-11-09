using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models;
using RaceDay.Web.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Shared helpers for every controller.</summary>
public abstract class AppController : Controller
{
    protected readonly ApiService Api;

    protected AppController(ApiService api) { Api = api; }

    protected int CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId) ?? 0;
    protected string CurrentRole => HttpContext.Session.GetString(SessionKeys.Role) ?? "";

    /// <summary>Remembers who logged in so every page can show the right menu.</summary>
    protected void StartSession(UserInfo user)
    {
        string apiCookie = HttpContext.Session.GetString(SessionKeys.ApiCookie) ?? "";

        HttpContext.Session.Clear();
        HttpContext.Session.SetString(SessionKeys.ApiCookie, apiCookie);
        HttpContext.Session.SetInt32(SessionKeys.UserId, user.Id);
        HttpContext.Session.SetString(SessionKeys.Role, user.Role);
        HttpContext.Session.SetString(SessionKeys.Name, $"{user.FirstName} {user.LastName}");
        HttpContext.Session.SetString(SessionKeys.Picture, user.ProfilePictureUrl ?? "");
    }

    /// <summary>A page could not load its data: go to login if the session ended, otherwise show a friendly page.</summary>
    protected IActionResult ApiFail<T>(ApiResult<T> result)
    {
        if (result.Unauthorized) return SessionExpired();
        return View("~/Views/Shared/ApiError.cshtml", result.Error);
    }

    /// <summary>A button or form failed: show the message on the page the user came from.</summary>
    protected IActionResult RedirectWithError<T>(ApiResult<T> result, string action, string? controller = null, object? routeValues = null)
    {
        if (result.Unauthorized) return SessionExpired();
        TempData["Error"] = result.Error;
        return RedirectToAction(action, controller, routeValues);
    }

    protected IActionResult SessionExpired()
    {
        HttpContext.Session.Clear();
        TempData["Error"] = "Your session has ended. Please log in again.";
        return RedirectToAction("Login", "Account");
    }
}
