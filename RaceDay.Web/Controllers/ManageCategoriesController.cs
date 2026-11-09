using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models;
using RaceDay.Web.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Organiser: add and delete the categories of an event.</summary>
[SessionAuthorize(Roles.Organiser)]
public class ManageCategoriesController : AppController
{
    public ManageCategoriesController(ApiService api) : base(api) { }

    // Lists the categories of the event.
    public async Task<IActionResult> Index(int eventId)
    {
        var model = await LoadAsync(eventId);
        if (model.Result != null) return model.Result;

        return View(model.Data);
    }

    // Adds a new category to the event.
    [HttpPost]
    public async Task<IActionResult> Add(int eventId, CategoryFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            var model = await LoadAsync(eventId);
            if (model.Result != null) return model.Result;

            model.Data!.Form = form;
            return View("Index", model.Data);
        }

        var result = await Api.PostAsync<CategoryInfo>($"api/events/{eventId}/categories",
            new { name = form.Name, description = form.Description });
        if (!result.Success) return RedirectWithError(result, "Index", null, new { eventId });

        TempData["Success"] = "Category added.";
        return RedirectToAction("Index", new { eventId });
    }

    // Deletes a category that nobody has entered yet.
    [HttpPost]
    public async Task<IActionResult> Delete(int eventId, int id)
    {
        var result = await Api.DeleteAsync($"api/categories/{id}");
        if (!result.Success)
        {
            if (result.StatusCode == HttpStatusCode.Conflict)
            {
                TempData["Error"] = "This category cannot be deleted because participants are already enrolled in it.";
                return RedirectToAction("Index", new { eventId });
            }
            return RedirectWithError(result, "Index", null, new { eventId });
        }

        TempData["Success"] = "Category deleted.";
        return RedirectToAction("Index", new { eventId });
    }

    // Loads the event and its categories, or returns the page to show if something failed.
    private async Task<(CategoriesViewModel? Data, IActionResult? Result)> LoadAsync(int eventId)
    {
        var ev = await Api.GetAsync<EventInfo>($"api/events/{eventId}");
        if (ev.StatusCode == HttpStatusCode.NotFound) return (null, NotFound());
        if (!ev.Success) return (null, ApiFail(ev));
        if (ev.Data!.OrganiserId != CurrentUserId)
        {
            TempData["Error"] = "You can only manage your own events.";
            return (null, RedirectToAction("Index", "ManageEvents"));
        }

        var categories = await Api.GetAsync<List<CategoryInfo>>($"api/events/{eventId}/categories");
        if (!categories.Success) return (null, ApiFail(categories));

        return (new CategoriesViewModel { Event = ev.Data, Categories = categories.Data! }, null);
    }
}
