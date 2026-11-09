using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Participants enter events; organisers confirm the entries.</summary>
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class EnrolmentsController : ApiControllerBase
{
    private readonly AppDbContext db;
    public EnrolmentsController(AppDbContext db) { this.db = db; }

    // Loads an enrolment together with its event, category and participant (needed for the DTO).
    private IQueryable<Enrolment> Full() => db.Enrolments
        .Include(e => e.Event)
        .Include(e => e.Category)
        .Include(e => e.Participant);

    /// <summary>Enters the logged-in participant into an event.</summary>
    /// <param name="eventId" example="1">The Id of the event to enter.</param>
    /// <param name="dto">The Id of the category the participant wants to enter.</param>
    /// <response code="201">The new enrolment. Its status starts as Pending.</response>
    /// <response code="400">The event is in the past, or the category is not part of this event.</response>
    /// <response code="403">The logged-in user is not a participant.</response>
    /// <response code="404">There is no event with that Id.</response>
    /// <response code="409">The participant is already entered in this event.</response>
    [HttpPost("api/events/{eventId:int}/enrolments")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrolmentDto>> Enrol(int eventId, EnrolRequestDto dto)
    {
        var item = await db.Events.FindAsync(eventId);
        if (item == null) return NotFound();
        if (item.EventDate < DateTime.UtcNow) return BadRequest(new ErrorDto("Event has already taken place."));

        bool categoryExists = await db.Categories.AnyAsync(c => c.Id == dto.CategoryId && c.EventId == eventId);
        if (!categoryExists) return BadRequest(new ErrorDto("Category does not belong to this event."));

        bool alreadyEnrolled = await db.Enrolments.AnyAsync(e => e.EventId == eventId && e.ParticipantId == CurrentUserId);
        if (alreadyEnrolled) return Conflict(new ErrorDto("You are already enrolled."));

        var enrolment = new Enrolment
        {
            EventId = eventId,
            CategoryId = dto.CategoryId,
            ParticipantId = CurrentUserId
        };

        db.Enrolments.Add(enrolment);
        await db.SaveChangesAsync();

        var created = await Full().SingleAsync(e => e.Id == enrolment.Id);
        return Created("/api/enrolments/mine", Mapping.ToDto(created));
    }

    /// <summary>Lists everyone who entered an event. Only the event's organiser can do this.</summary>
    /// <param name="eventId" example="1">The Id of the event.</param>
    /// <param name="status">Optional. Only show Pending or Confirmed enrolments.</param>
    /// <response code="200">The enrolments, oldest first.</response>
    /// <response code="403">The event belongs to another organiser.</response>
    /// <response code="404">There is no event with that Id.</response>
    [HttpGet("api/events/{eventId:int}/enrolments")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<EnrolmentDto>>> GetForEvent(int eventId, EnrolmentStatus? status)
    {
        var item = await db.Events.FindAsync(eventId);
        if (item == null) return NotFound();
        if (item.OrganiserId != CurrentUserId) return Forbid();

        var query = Full().Where(e => e.EventId == eventId);
        if (status.HasValue) query = query.Where(e => e.Status == status.Value);

        var list = await query.OrderBy(e => e.EnrolledAt).ToListAsync();
        return Ok(list.Select(Mapping.ToDto));
    }

    /// <summary>Lists the events the logged-in participant has entered.</summary>
    /// <response code="200">The enrolments, earliest event first.</response>
    /// <response code="403">The logged-in user is not a participant.</response>
    [HttpGet("api/enrolments/mine")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<EnrolmentDto>>> GetMine()
    {
        var list = await Full().Where(e => e.ParticipantId == CurrentUserId)
            .OrderBy(e => e.Event!.EventDate).ToListAsync();
        return Ok(list.Select(Mapping.ToDto));
    }

    /// <summary>Confirms an enrolment (or sets it back to Pending). Only the event's organiser can do this.</summary>
    /// <param name="id" example="1">The Id of the enrolment.</param>
    /// <param name="dto">The new status: Pending or Confirmed.</param>
    /// <response code="200">The updated enrolment.</response>
    /// <response code="403">The event belongs to another organiser.</response>
    /// <response code="404">There is no enrolment with that Id.</response>
    [HttpPut("api/enrolments/{id:int}/status")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnrolmentDto>> SetStatus(int id, EnrolmentStatusDto dto)
    {
        var enrolment = await Full().SingleOrDefaultAsync(e => e.Id == id);
        if (enrolment == null) return NotFound();
        if (enrolment.Event!.OrganiserId != CurrentUserId) return Forbid();

        enrolment.Status = dto.Status;
        await db.SaveChangesAsync();
        return Ok(Mapping.ToDto(enrolment));
    }

    /// <summary>Withdraws the logged-in participant from an event. Any recorded result is removed too.</summary>
    /// <param name="id" example="1">The Id of the enrolment to withdraw.</param>
    /// <response code="204">The enrolment was removed.</response>
    /// <response code="403">The enrolment belongs to another participant.</response>
    /// <response code="404">There is no enrolment with that Id.</response>
    [HttpDelete("api/enrolments/{id:int}")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Withdraw(int id)
    {
        var enrolment = await db.Enrolments.Include(e => e.Result).SingleOrDefaultAsync(e => e.Id == id);
        if (enrolment == null) return NotFound();
        if (enrolment.ParticipantId != CurrentUserId) return Forbid();

        if (enrolment.Result != null) db.Results.Remove(enrolment.Result);
        db.Enrolments.Remove(enrolment);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
