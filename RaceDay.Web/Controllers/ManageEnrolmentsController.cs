using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models;
using RaceDay.Web.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Organiser: see who entered an event and confirm them.</summary>
[SessionAuthorize(Roles.Organiser)]
public class ManageEnrolmentsController : AppController
{
    public ManageEnrolmentsController(ApiService api) : base(api) { }

    // Lists the enrolments of the event.
    public async Task<IActionResult> Index(int eventId)
    {
        var ev = await Api.GetAsync<EventInfo>($"api/events/{eventId}");
        if (ev.StatusCode == HttpStatusCode.NotFound) return NotFound();
        if (!ev.Success) return ApiFail(ev);

        var enrolments = await Api.GetAsync<List<EnrolmentInfo>>($"api/events/{eventId}/enrolments");
        if (!enrolments.Success) return ApiFail(enrolments);

        return View(new EnrolmentsViewModel { Event = ev.Data!, Enrolments = enrolments.Data! });
    }

    // Confirms an enrolment or sets it back to Pending.
    [HttpPost]
    public async Task<IActionResult> SetStatus(int eventId, int id, string status)
    {
        if (status != "Pending" && status != "Confirmed")
        {
            TempData["Error"] = "That status is not valid.";
            return RedirectToAction("Index", new { eventId });
        }

        var result = await Api.PutAsync<EnrolmentInfo>($"api/enrolments/{id}/status", new { status });
        if (!result.Success) return RedirectWithError(result, "Index", null, new { eventId });

        TempData["Success"] = $"Enrolment marked as {status}.";
        return RedirectToAction("Index", new { eventId });
    }
}
