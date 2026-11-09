using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models;
using RaceDay.Web.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Participant pages: browse events, see the details and enter an event.</summary>
[SessionAuthorize]
public class EventsController : AppController
{
    public EventsController(ApiService api) : base(api) { }

    // Home page for participants: upcoming events with search and filters.
    // The API does the filtering; this method only builds the query string.
    public async Task<IActionResult> Index(string? search, string? type, string? distance)
    {
        var filters = new List<string> { "upcomingOnly=true" };

        if (type is "Run" or "Walk" or "Cycle")
            filters.Add($"type={type}");

        if (!string.IsNullOrWhiteSpace(search))
            filters.Add($"search={Uri.EscapeDataString(search.Trim())}");

        // short = up to 5 km, medium = 5 to 21.1 km, long = over 21.1 km
        if (distance == "short") filters.Add("maxDistanceKm=5");
        if (distance == "medium") { filters.Add("minDistanceKm=5.01"); filters.Add("maxDistanceKm=21.1"); }
        if (distance == "long") filters.Add("minDistanceKm=21.11");

        var result = await Api.GetAsync<List<EventInfo>>("api/events?" + string.Join("&", filters));
        if (!result.Success) return ApiFail(result);

        // Second call, without filters, only to show "X upcoming events" in the page heading.
        var all = await Api.GetAsync<List<EventInfo>>("api/events?upcomingOnly=true");
        int totalUpcoming = all.Success ? all.Data!.Count : result.Data!.Count;

        return View(new EventBrowseViewModel
        {
            Events = result.Data!,
            Search = search,
            Type = type,
            Distance = distance,
            TotalUpcoming = totalUpcoming
        });
    }

    // Shows one event with its categories and the user's own enrolment.
    public async Task<IActionResult> Details(int id)
    {
        var eventResult = await Api.GetAsync<EventInfo>($"api/events/{id}");
        if (eventResult.StatusCode == HttpStatusCode.NotFound) return NotFound();
        if (!eventResult.Success) return ApiFail(eventResult);

        var categories = await Api.GetAsync<List<CategoryInfo>>($"api/events/{id}/categories");
        if (!categories.Success) return ApiFail(categories);

        var model = new EventDetailsViewModel { Event = eventResult.Data!, Categories = categories.Data! };

        if (CurrentRole == Roles.Participant)
        {
            var mine = await Api.GetAsync<List<EnrolmentInfo>>("api/enrolments/mine");
            if (!mine.Success) return ApiFail(mine);
            model.MyEnrolment = mine.Data!.FirstOrDefault(e => e.EventId == id);
        }

        return View(model);
    }

    // Enters the logged-in participant into the chosen category.
    [HttpPost]
    [SessionAuthorize(Roles.Participant)]
    public async Task<IActionResult> Enrol(int id, int categoryId)
    {
        if (categoryId <= 0)
        {
            TempData["Error"] = "Please choose a category before entering the event.";
            return RedirectToAction("Details", new { id });
        }

        var result = await Api.PostAsync<EnrolmentInfo>($"api/events/{id}/enrolments", new { categoryId });
        if (!result.Success) return RedirectWithError(result, "Details", null, new { id });

        TempData["Success"] = "You are entered! The organiser will confirm your enrolment.";
        return RedirectToAction("Index", "MyEvents");
    }
}
