using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Register, log in and log out.</summary>
[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly AppDbContext db;
    private readonly IPasswordHasher<AppUser> hasher;
    private readonly IConfiguration config;

    public AuthController(AppDbContext db, IPasswordHasher<AppUser> hasher, IConfiguration config)
    {
        this.db = db;
        this.hasher = hasher;
        this.config = config;
    }

    /// <summary>Creates a new Organiser or Participant account.</summary>
    /// <param name="dto">The new user's details. Role must be "Organiser" or "Participant".</param>
    /// <response code="201">The account was created.</response>
    /// <response code="400">Invalid details, a wrong role, or a wrong organiser code.</response>
    /// <response code="409">That email address is already registered.</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Register(RegisterDto dto)
    {
        // The role must be one of the two roles we have.
        if (!Roles.IsValid(dto.Role))
            return BadRequest(new ErrorDto("Role must be Organiser or Participant."));

        // If the API has an organiser code set, organisers must supply it.
        string? requiredCode = config["OrganiserRegistrationCode"];
        if (dto.Role == Roles.Organiser && !string.IsNullOrEmpty(requiredCode) && dto.OrganiserCode != requiredCode)
            return BadRequest(new ErrorDto("A valid organiser code is required."));

        // Emails are stored in lower case so "A@x.com" and "a@x.com" are the same user.
        string email = dto.Email.Trim().ToLower();
        if (await db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new ErrorDto("Email already exists."));

        Role role = await db.Roles.SingleAsync(r => r.Name == dto.Role);
        var user = new AppUser
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = email,
            PhoneNumber = dto.PhoneNumber,
            RoleId = role.Id,
            Role = role
        };

        // Only the hash is stored, never the real password.
        user.PasswordHash = hasher.HashPassword(user, dto.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return Created("/api/users/me", Mapping.ToDto(user));
    }

    /// <summary>Logs a user in. The API sets a session cookie that is used by all the other endpoints.</summary>
    /// <param name="dto">The email and password. Set RememberMe to true to stay logged in for 7 days.</param>
    /// <response code="200">Logged in. The user's details are returned.</response>
    /// <response code="401">The email or password is wrong.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorDto), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> Login(LoginDto dto)
    {
        string email = dto.Email.Trim().ToLower();
        var user = await db.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.Email == email);

        // The same message is used for "no such user" and "wrong password" on purpose.
        if (user == null || hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password) == PasswordVerificationResult.Failed)
            return Unauthorized(new ErrorDto("Invalid email or password."));

        // Start a fresh session and remember who the user is.
        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32(SessionAuth.UserId, user.Id);
        HttpContext.Session.SetString(SessionAuth.Role, user.Role!.Name);
        HttpContext.Session.SetString(SessionAuth.Name, Mapping.FullName(user));
        HttpContext.Session.SetInt32(SessionAuth.RememberMe, dto.RememberMe ? 1 : 0);
        HttpContext.Session.SetString(SessionAuth.LastSeen, DateTime.UtcNow.Ticks.ToString());

        return Ok(Mapping.ToDto(user));
    }

    /// <summary>Logs the current user out by clearing the session.</summary>
    /// <response code="204">Logged out.</response>
    /// <response code="401">Nobody is logged in.</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return NoContent();
    }
}
