using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Storage;

namespace RaceDay.Api.Controllers;

/// <summary>The logged-in user's own profile.</summary>
[Route("api/users")]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class UsersController : ApiControllerBase
{
    private readonly AppDbContext db;
    private readonly IBlobStorage storage;

    public UsersController(AppDbContext db, IBlobStorage storage)
    {
        this.db = db;
        this.storage = storage;
    }

    /// <summary>Gets the profile of the logged-in user.</summary>
    /// <response code="200">The user's profile.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetMe()
    {
        var user = await db.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.Id == CurrentUserId);
        if (user == null) return NotFound();
        return Ok(Mapping.ToDto(user));
    }

    /// <summary>Changes the name and phone number of the logged-in user.</summary>
    /// <param name="dto">The new first name, last name and phone number.</param>
    /// <response code="200">The updated profile.</response>
    /// <response code="400">Invalid details.</response>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> UpdateMe(UpdateProfileDto dto)
    {
        var user = await db.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.Id == CurrentUserId);
        if (user == null) return NotFound();

        user.FirstName = dto.FirstName.Trim();
        user.LastName = dto.LastName.Trim();
        user.PhoneNumber = dto.PhoneNumber;
        await db.SaveChangesAsync();

        return Ok(Mapping.ToDto(user));
    }

    /// <summary>Uploads a profile picture to Azure Blob Storage.</summary>
    /// <param name="file">An image file (JPG, PNG, GIF or WEBP) smaller than 5 MB.</param>
    /// <response code="200">The profile with the new picture link.</response>
    /// <response code="400">No file, wrong file type, or the file is too big.</response>
    /// <response code="503">Azure Blob Storage is not set up or could not be reached.</response>
    [HttpPost("me/picture")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<UserDto>> UploadPicture(IFormFile file)
    {
        var user = await db.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.Id == CurrentUserId);
        if (user == null) return NotFound();

        string? problem = ImageUpload.Validate(file);
        if (problem != null) return BadRequest(new ErrorDto(problem));

        try
        {
            user.ProfilePictureUrl = await storage.UploadImageAsync(file, "profiles");
        }
        catch (BlobStorageException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ErrorDto(ex.Message));
        }

        await db.SaveChangesAsync();
        return Ok(Mapping.ToDto(user));
    }
}
