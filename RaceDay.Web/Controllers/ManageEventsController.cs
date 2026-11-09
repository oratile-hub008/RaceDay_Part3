using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models;
using RaceDay.Web.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Organiser: create, edit and delete events (with a banner image).</summary>
[SessionAuthorize(Roles.Organiser)]
public class ManageEventsController : AppController
{
    public ManageEventsController(ApiService api) : base(api) { }

    // Lists the organiser's own events.
    public async Task<IActionResult> Index()
    {
        var result = await Api.GetAsync<List<EventInfo>>("api/events/mine");
        if (!result.Success) return ApiFail(result);

        return View(result.Data!);
    }

    // Shows an empty event form.
    [HttpGet]
    public IActionResult Create() =>
        View("Form", new EventFormViewModel { EventDate = DateTime.Today.AddDays(30).AddHours(8) });

    // Saves a new event and uploads its banner if one was chosen.
    [HttpPost]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        if (model.EventDate < DateTime.Now)
            ModelState.AddModelError(nameof(model.EventDate), "The event date must be in the future.");
        if (!ModelState.IsValid) return View("Form", model);

        var created = await Api.PostAsync<EventInfo>("api/events", ToRequest(model));
        if (!created.Success)
        {
            ModelState.AddModelError("", created.Error);
            return View("Form", model);
        }

        return await FinishSaveAsync(created.Data!.Id, model.Banner, "Event created.");
    }

    // Shows the event form filled with the current details.
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await Api.GetAsync<EventInfo>($"api/events/{id}");
        if (!result.Success) return ApiFail(result);

        var ev = result.Data!;
        if (ev.OrganiserId != CurrentUserId)
        {
            TempData["Error"] = "You can only edit your own events.";
            return RedirectToAction("Index");
        }

        return View("Form", new EventFormViewModel
        {
            Id = ev.Id,
            Name = ev.Name,
            Description = ev.Description,
            EventDate = ev.EventDate,
            Location = ev.Location,
            DistanceKm = ev.DistanceKm,
            EventType = ev.EventType,
            ExistingBannerUrl = ev.BannerUrl
        });
    }

    // Saves the changes and uploads a new banner if one was chosen.
    [HttpPost]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model)
    {
        model.Id = id;
        if (!ModelState.IsValid) return View("Form", model);

        var updated = await Api.PutAsync<EventInfo>($"api/events/{id}", ToRequest(model));
        if (!updated.Success)
        {
            if (updated.Unauthorized) return SessionExpired();
            ModelState.AddModelError("", updated.Error);
            model.ExistingBannerUrl = null;
            return View("Form", model);
        }

        model.ExistingBannerUrl = updated.Data!.BannerUrl;
        return await FinishSaveAsync(id, model.Banner, "Event updated.");
    }

    // Deletes the event.
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await Api.DeleteAsync($"api/events/{id}");
        if (!result.Success) return RedirectWithError(result, "Index");

        TempData["Success"] = "Event deleted.";
        return RedirectToAction("Index");
    }

    // Uploads the banner (if one was chosen) after the event has been saved.
    private async Task<IActionResult> FinishSaveAsync(int eventId, IFormFile? banner, string successMessage)
    {
        if (banner != null && banner.Length > 0)
        {
            var upload = await Api.UploadAsync<EventInfo>($"api/events/{eventId}/banner", banner);
            if (!upload.Success)
            {
                if (upload.Unauthorized) return SessionExpired();
                TempData["Warning"] = $"{successMessage} The banner could not be uploaded: {upload.Error}";
                return RedirectToAction("Index");
            }
        }

        TempData["Success"] = successMessage;
        return RedirectToAction("Index");
    }

    private static object ToRequest(EventFormViewModel model) => new
    {
        name = model.Name,
        description = model.Description ?? "",
        eventDate = model.EventDate,
        location = model.Location,
        distanceKm = model.DistanceKm,
        eventType = model.EventType
    };
}
