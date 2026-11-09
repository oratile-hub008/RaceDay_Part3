using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>The categories (such as "10km Open") that participants can choose when entering an event.</summary>
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class CategoriesController : ApiControllerBase
{
    private readonly AppDbContext db;
    public CategoriesController(AppDbContext db) { this.db = db; }

    /// <summary>Lists the categories of one event.</summary>
    /// <param name="eventId" example="1">The Id of the event.</param>
    /// <response code="200">The categories, sorted by name.</response>
    /// <response code="404">There is no event with that Id.</response>
    [HttpGet("api/events/{eventId:int}/categories")]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetForEvent(int eventId)
    {
        if (!await db.Events.AnyAsync(e => e.Id == eventId)) return NotFound();

        var categories = await db.Categories.Where(c => c.EventId == eventId).OrderBy(c => c.Name).ToListAsync();
        return Ok(categories.Select(Mapping.ToDto));
    }

    /// <summary>Adds a category to an event. Only the event's organiser can do this.</summary>
    /// <param name="eventId" example="1">The Id of the event.</param>
    /// <param name="dto">The category name and description.</param>
    /// <response code="201">The new category.</response>
    /// <response code="403">The event belongs to another organiser.</response>
    /// <response code="404">There is no event with that Id.</response>
    [HttpPost("api/events/{eventId:int}/categories")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> Create(int eventId, CategoryRequestDto dto)
    {
        var item = await db.Events.FindAsync(eventId);
        if (item == null) return NotFound();
        if (item.OrganiserId != CurrentUserId) return Forbid();

        var category = new Category
        {
            EventId = eventId,
            Name = dto.Name.Trim(),
            Description = dto.Description
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return Created($"/api/events/{eventId}/categories", Mapping.ToDto(category));
    }

    /// <summary>Changes a category. Only the event's organiser can do this.</summary>
    /// <param name="id" example="1">The Id of the category.</param>
    /// <param name="dto">The new name and description.</param>
    /// <response code="200">The updated category.</response>
    /// <response code="403">The event belongs to another organiser.</response>
    /// <response code="404">There is no category with that Id.</response>
    [HttpPut("api/categories/{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> Update(int id, CategoryRequestDto dto)
    {
        var category = await db.Categories.Include(c => c.Event).SingleOrDefaultAsync(c => c.Id == id);
        if (category == null) return NotFound();
        if (category.Event!.OrganiserId != CurrentUserId) return Forbid();

        category.Name = dto.Name.Trim();
        category.Description = dto.Description;
        await db.SaveChangesAsync();
        return Ok(Mapping.ToDto(category));
    }

    /// <summary>Deletes a category that nobody has entered yet.</summary>
    /// <param name="id" example="1">The Id of the category.</param>
    /// <response code="204">The category was deleted.</response>
    /// <response code="403">The event belongs to another organiser.</response>
    /// <response code="404">There is no category with that Id.</response>
    /// <response code="409">Participants have already entered this category, so it cannot be deleted.</response>
    [HttpDelete("api/categories/{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await db.Categories.Include(c => c.Event).SingleOrDefaultAsync(c => c.Id == id);
        if (category == null) return NotFound();
        if (category.Event!.OrganiserId != CurrentUserId) return Forbid();
        if (await db.Enrolments.AnyAsync(e => e.CategoryId == id)) return Conflict();

        db.Categories.Remove(category);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
