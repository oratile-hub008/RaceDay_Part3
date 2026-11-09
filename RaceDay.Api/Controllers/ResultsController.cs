using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Organisers record finish times and positions; participants view their own results.</summary>
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class ResultsController : ApiControllerBase
{
    private readonly AppDbContext db;
    public ResultsController(AppDbContext db) { this.db = db; }

    // Loads a result together with its enrolment, event, category and participant (needed for the DTO).
    private IQueryable<Result> Full() => db.Results
        .Include(r => r.Enrolment!).ThenInclude(e => e!.Event)
        .Include(r => r.Enrolment!).ThenInclude(e => e!.Category)
        .Include(r => r.Enrolment!).ThenInclude(e => e!.Participant);

    // How many participants have a recorded result for each of the given events.
    private async Task<Dictionary<int, int>> FinisherCounts(IEnumerable<int> eventIds)
    {
        var ids = eventIds.Distinct().ToList();
        var counts = await db.Results
            .Where(r => ids.Contains(r.Enrolment!.EventId))
            .GroupBy(r => r.Enrolment!.EventId)
            .Select(g => new { EventId = g.Key, Count = g.Count() })
            .ToListAsync();

        return counts.ToDictionary(c => c.EventId, c => c.Count);
    }

    // Turns one result into a DTO and fills in how many people finished that event.
    private async Task<ResultDto> ToDtoWithTotal(Result result)
    {
        var counts = await FinisherCounts(new[] { result.Enrolment!.EventId });
        return Mapping.ToDto(result) with { TotalFinishers = counts.GetValueOrDefault(result.Enrolment.EventId) };
    }

    /// <summary>Records the result of one participant. Only the event's organiser can do this.</summary>
    /// <param name="enrolmentId" example="1">The Id of the enrolment the result belongs to.</param>
    /// <param name="dto">The finish time (for example 00:52:30) and the finishing position.</param>
    /// <response code="201">The new result.</response>
    /// <response code="403">The event belongs to another organiser.</response>
    /// <response code="404">There is no enrolment with that Id.</response>
    /// <response code="409">A result has already been recorded for this enrolment. Use PUT to change it.</response>
    [HttpPost("api/enrolments/{enrolmentId:int}/result")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResultDto>> Record(int enrolmentId, ResultRequestDto dto)
    {
        var enrolment = await db.Enrolments.Include(e => e.Event).SingleOrDefaultAsync(e => e.Id == enrolmentId);
        if (enrolment == null) return NotFound();
        if (enrolment.Event!.OrganiserId != CurrentUserId) return Forbid();
        if (await db.Results.AnyAsync(r => r.EnrolmentId == enrolmentId)) return Conflict();

        var result = new Result
        {
            EnrolmentId = enrolmentId,
            FinishTime = dto.FinishTime,
            Position = dto.Position
        };

        db.Results.Add(result);
        await db.SaveChangesAsync();

        var created = await Full().SingleAsync(r => r.Id == result.Id);
        return Created($"/api/events/{enrolment.EventId}/results", await ToDtoWithTotal(created));
    }

    /// <summary>Changes a recorded result. Only the event's organiser can do this.</summary>
    /// <param name="id" example="1">The Id of the result.</param>
    /// <param name="dto">The new finish time and position.</param>
    /// <response code="200">The updated result.</response>
    /// <response code="403">The event belongs to another organiser.</response>
    /// <response code="404">There is no result with that Id.</response>
    [HttpPut("api/results/{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultDto>> Update(int id, ResultRequestDto dto)
    {
        var result = await Full().SingleOrDefaultAsync(r => r.Id == id);
        if (result == null) return NotFound();
        if (result.Enrolment!.Event!.OrganiserId != CurrentUserId) return Forbid();

        result.FinishTime = dto.FinishTime;
        result.Position = dto.Position;
        await db.SaveChangesAsync();
        return Ok(await ToDtoWithTotal(result));
    }

    /// <summary>Lists all the results of one event. Only the event's organiser can do this.</summary>
    /// <param name="eventId" example="1">The Id of the event.</param>
    /// <response code="200">The results, first place first.</response>
    /// <response code="403">The event belongs to another organiser.</response>
    /// <response code="404">There is no event with that Id.</response>
    [HttpGet("api/events/{eventId:int}/results")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ResultDto>>> GetForEvent(int eventId)
    {
        var item = await db.Events.FindAsync(eventId);
        if (item == null) return NotFound();
        if (item.OrganiserId != CurrentUserId) return Forbid();

        var list = await Full().Where(r => r.Enrolment!.EventId == eventId)
            .OrderBy(r => r.Position).ToListAsync();
        return Ok(list.Select(r => Mapping.ToDto(r) with { TotalFinishers = list.Count }));
    }

    /// <summary>Lists the results of the logged-in participant.</summary>
    /// <response code="200">The results, most recent event first.</response>
    /// <response code="403">The logged-in user is not a participant.</response>
    [HttpGet("api/results/mine")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(typeof(IEnumerable<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ResultDto>>> GetMine()
    {
        var list = await Full().Where(r => r.Enrolment!.ParticipantId == CurrentUserId)
            .OrderByDescending(r => r.Enrolment!.Event!.EventDate).ToListAsync();

        var counts = await FinisherCounts(list.Select(r => r.Enrolment!.EventId));
        return Ok(list.Select(r => Mapping.ToDto(r) with { TotalFinishers = counts.GetValueOrDefault(r.Enrolment!.EventId) }));
    }
}
