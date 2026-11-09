using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models;
using RaceDay.Web.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Participant: the events I have entered.</summary>
[SessionAuthorize(Roles.Participant)]
public class MyEventsController : AppController
{
    public MyEventsController(ApiService api) : base(api) { }

    // Lists my enrolments.
    public async Task<IActionResult> Index()
    {
        var result = await Api.GetAsync<List<EnrolmentInfo>>("api/enrolments/mine");
        if (!result.Success) return ApiFail(result);

        return View(result.Data!.OrderBy(e => e.EventDate).ToList());
    }

    // Removes my enrolment from an event.
    [HttpPost]
    public async Task<IActionResult> Withdraw(int id)
    {
        var result = await Api.DeleteAsync($"api/enrolments/{id}");
        if (!result.Success) return RedirectWithError(result, "Index");

        TempData["Success"] = "You have withdrawn from the event.";
        return RedirectToAction("Index");
    }
}
