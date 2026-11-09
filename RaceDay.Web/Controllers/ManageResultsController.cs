using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models;
using RaceDay.Web.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Organiser: capture finish times and positions.</summary>
[SessionAuthorize(Roles.Organiser)]
public class ManageResultsController : AppController
{
    public ManageResultsController(ApiService api) : base(api) { }

    // Lists the confirmed participants with any result already recorded.
    public async Task<IActionResult> Index(int eventId)
    {
        var ev = await Api.GetAsync<EventInfo>($"api/events/{eventId}");
        if (ev.StatusCode == HttpStatusCode.NotFound) return NotFound();
        if (!ev.Success) return ApiFail(ev);

        var enrolments = await Api.GetAsync<List<EnrolmentInfo>>($"api/events/{eventId}/enrolments");
        if (!enrolments.Success) return ApiFail(enrolments);

        var results = await Api.GetAsync<List<ResultInfo>>($"api/events/{eventId}/results");
        if (!results.Success) return ApiFail(results);

        var rows = enrolments.Data!
            .Select(e => new ResultRow
            {
                Enrolment = e,
                Result = results.Data!.FirstOrDefault(r => r.EnrolmentId == e.Id)
            })
            .OrderBy(r => r.Result == null)
            .ThenBy(r => r.Result?.Position ?? 0)
            .ThenBy(r => r.Enrolment.ParticipantName)
            .ToList();

        return View(new ResultsViewModel { Event = ev.Data!, Rows = rows });
    }

    // Creates the result, or updates it if one already exists.
    [HttpPost]
    public async Task<IActionResult> Save(ResultFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" ", ModelState.Values
                .SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Where(m => m.Length > 0));
            return RedirectToAction("Index", new { eventId = model.EventId });
        }

        var body = new { finishTime = model.FinishTime, position = model.Position };

        var result = model.ResultId.HasValue
            ? await Api.PutAsync<ResultInfo>($"api/results/{model.ResultId.Value}", body)
            : await Api.PostAsync<ResultInfo>($"api/enrolments/{model.EnrolmentId}/result", body);

        if (!result.Success) return RedirectWithError(result, "Index", null, new { eventId = model.EventId });

        TempData["Success"] = "Result saved and published.";
        return RedirectToAction("Index", new { eventId = model.EventId });
    }
}
