using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models;
using RaceDay.Web.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Participant: my finish times and positions.</summary>
[SessionAuthorize(Roles.Participant)]
public class MyResultsController : AppController
{
    public MyResultsController(ApiService api) : base(api) { }

    // Lists my results.
    public async Task<IActionResult> Index()
    {
        var result = await Api.GetAsync<List<ResultInfo>>("api/results/mine");
        if (!result.Success) return ApiFail(result);

        return View(result.Data!.OrderByDescending(r => r.EventDate).ToList());
    }
}
