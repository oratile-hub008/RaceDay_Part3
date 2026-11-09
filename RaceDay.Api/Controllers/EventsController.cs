using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Storage;

namespace RaceDay.Api.Controllers;

/// <summary>Create, find, change and delete events.</summary>
[Route("api/events")]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class EventsController : ApiControllerBase
{
    private readonly AppDbContext db;
    private readonly IBlobStorage storage;

    public EventsController(AppDbContext db, IBlobStorage storage)
    {
        this.db = db;
        this.storage = storage;
    }

    /// <summary>Lists events. All the filters are optional and can be combined.</summary>
    /// <param name="type">Only show this type of event: Run, Walk or Cycle.</param>
    /// <param name="search">Only show events whose name or location contains this text, for example "soweto".</param>
    /// <param name="upcomingOnly">true = hide events that have already taken place. The default is false.</param>
    /// <param name="minDistanceKm">Only show events at least this many kilometres long, for example 5.</param>
    /// <param name="maxDistanceKm">Only show events at most this many kilometres long, for example 21.1.</param>
    /// <response code="200">The matching events, earliest first.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetAll(
        EventType? type, string? search, bool upcomingOnly = false,
        decimal? minDistanceKm = null, decimal? maxDistanceKm = null)
    {
        var query = db.Events.Include(e => e.Organiser).AsQueryable();

        // Each filter only runs when the caller supplied a value.
        if (type.HasValue)
            query = query.Where(e => e.EventType == type.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim();
            query = query.Where(e => e.Name.Contains(term) || e.Location.Contains(term));
        }

        if (upcomingOnly)
            query = query.Where(e => e.EventDate >= DateTime.UtcNow);

        if (minDistanceKm.HasValue)
            query = query.Where(e => e.DistanceKm >= minDistanceKm.Value);

        if (maxDistanceKm.HasValue)
            query = query.Where(e => e.DistanceKm <= maxDistanceKm.Value);

        var events = await query.OrderBy(e => e.EventDate).ToListAsync();
        return Ok(events.Select(Mapping.ToDto));
    }

    /// <summary>Lists only the events created by the logged-in organiser.</summary>
    /// <response code="200">The organiser's events, earliest first.</response>
    /// <response code="403">The logged-in user is not an organiser.</response>
    [HttpGet("mine")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetMine()
    {
        var events = await db.Events.Include(e => e.Organiser)
            .Where(e => e.OrganiserId == CurrentUserId)
            .OrderBy(e => e.EventDate).ToListAsync();

        return Ok(events.Select(Mapping.ToDto));
    }

    /// <summary>Gets one event.</summary>
    /// <param name="id" example="1">The Id of the event.</param>
    /// <response code="200">The event.</response>
    /// <response code="404">There is no event with that Id.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> GetById(int id)
    {
        var item = await db.Events.Include(e => e.Organiser).SingleOrDefaultAsync(e => e.Id == id);
        if (item == null) return NotFound();
        return Ok(Mapping.ToDto(item));
    }

    /// <summary>Creates an event. The logged-in organiser becomes its owner.</summary>
    /// <param name="dto">The event details.</param>
    /// <response code="201">The new event.</response>
    /// <response code="400">Invalid details.</response>
    /// <response code="403">The logged-in user is not an organiser.</response>
    [HttpPost]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventDto>> Create(EventRequestDto dto)
    {
        var item = new Event
        {
            Name = dto.Name.Trim(),
            Description = dto.Description,
            EventDate = dto.EventDate,
            Location = dto.Location.Trim(),
            DistanceKm = dto.DistanceKm,
            EventType = dto.EventType,
            OrganiserId = CurrentUserId
        };

        db.Events.Add(item);
        await db.SaveChangesAsync();
        await db.Entry(item).Reference(e => e.Organiser).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = item.Id }, Mapping.ToDto(item));
    }

    /// <summary>Changes an event. Only the organiser who created it can do this.</summary>
    /// <param name="id" example="1">The Id of the event to change.</param>
    /// <param name="dto">The new event details.</param>
    /// <response code="200">The updated event.</response>
    /// <response code="403">The event belongs to another organiser.</response>
    /// <response code="404">There is no event with that Id.</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> Update(int id, EventRequestDto dto)
    {
        var item = await db.Events.Include(e => e.Organiser).SingleOrDefaultAsync(e => e.Id == id);
        if (item == null) return NotFound();
        if (item.OrganiserId != CurrentUserId) return Forbid();

        item.Name = dto.Name.Trim();
        item.Description = dto.Description;
        item.EventDate = dto.EventDate;
        item.Location = dto.Location.Trim();
        item.DistanceKm = dto.DistanceKm;
        item.EventType = dto.EventType;

        await db.SaveChangesAsync();
        return Ok(Mapping.ToDto(item));
    }

    /// <summary>Uploads a banner image for an event to Azure Blob Storage.</summary>
    /// <param name="id" example="1">The Id of the event.</param>
    /// <param name="file">An image file (JPG, PNG, GIF or WEBP) smaller than 5 MB.</param>
    /// <response code="200">The event with the new banner link.</response>
    /// <response code="400">No file, wrong file type, or the file is too big.</response>
    /// <response code="403">The event belongs to another organiser.</response>
    /// <response code="404">There is no event with that Id.</response>
    /// <response code="503">Azure Blob Storage is not set up or could not be reached.</response>
    [HttpPost("{id:int}/banner")]
    [Authorize(Roles = Roles.Organiser)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<EventDto>> UploadBanner(int id, IFormFile file)
    {
        var item = await db.Events.Include(e => e.Organiser).SingleOrDefaultAsync(e => e.Id == id);
        if (item == null) return NotFound();
        if (item.OrganiserId != CurrentUserId) return Forbid();

        string? problem = ImageUpload.Validate(file);
        if (problem != null) return BadRequest(new ErrorDto(problem));

        try
        {
            item.BannerUrl = await storage.UploadImageAsync(file, "banners");
        }
        catch (BlobStorageException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ErrorDto(ex.Message));
        }

        await db.SaveChangesAsync();
        return Ok(Mapping.ToDto(item));
    }

    /// <summary>Deletes an event together with its categories, enrolments and results.</summary>
    /// <param name="id" example="1">The Id of the event to delete.</param>
    /// <response code="204">The event was deleted.</response>
    /// <response code="403">The event belongs to another organiser.</response>
    /// <response code="404">There is no event with that Id.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.Events.SingleOrDefaultAsync(e => e.Id == id);
        if (item == null) return NotFound();
        if (item.OrganiserId != CurrentUserId) return Forbid();

        // Remove the child rows first (results, enrolments, categories), then the event itself.
        var enrolments = await db.Enrolments.Include(e => e.Result).Where(e => e.EventId == id).ToListAsync();
        db.Results.RemoveRange(enrolments.Where(e => e.Result != null).Select(e => e.Result!));
        db.Enrolments.RemoveRange(enrolments);
        db.Categories.RemoveRange(await db.Categories.Where(c => c.EventId == id).ToListAsync());
        db.Events.Remove(item);
        await db.SaveChangesAsync();

        return NoContent();
    }
}
