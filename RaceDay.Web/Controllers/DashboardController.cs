using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models;
using RaceDay.Web.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Organiser home page: a summary of their events and enrolments.</summary>
[SessionAuthorize(Roles.Organiser)]
public class DashboardController : AppController
{
    public DashboardController(ApiService api) : base(api) { }

    // Loads the organiser's events and counts the enrolments.
    public async Task<IActionResult> Index()
    {
        var events = await Api.GetAsync<List<EventInfo>>("api/events/mine");
        if (!events.Success) return ApiFail(events);

        var model = new DashboardViewModel
        {
            OrganiserName = HttpContext.Session.GetString(SessionKeys.Name) ?? ""
        };

        foreach (var ev in events.Data!)
        {
            var enrolments = await Api.GetAsync<List<EnrolmentInfo>>($"api/events/{ev.Id}/enrolments");
            if (!enrolments.Success) return ApiFail(enrolments);

            model.Events.Add(new EventSummary
            {
                Event = ev,
                Total = enrolments.Data!.Count,
                Pending = enrolments.Data.Count(e => e.Status == "Pending"),
                Confirmed = enrolments.Data.Count(e => e.Status == "Confirmed")
            });
        }

        return View(model);
    }
}
