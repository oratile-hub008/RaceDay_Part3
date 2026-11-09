using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models;
using RaceDay.Web.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Landing page and the friendly error pages.</summary>
public class HomeController : AppController
{
    public HomeController(ApiService api) : base(api) { }

    // Landing page for visitors; logged-in users go straight to their own home.
    public IActionResult Index()
    {
        if (CurrentUserId == 0) return View();

        return CurrentRole == Roles.Organiser
            ? RedirectToAction("Index", "Dashboard")
            : RedirectToAction("Index", "Events");
    }

    // Shown when an unexpected error happens.
    public IActionResult Error() => View();

    // Shown for 404 and other status codes.
    public IActionResult StatusPage(int code)
    {
        Response.StatusCode = code;
        ViewData["Code"] = code;
        return View();
    }
}
