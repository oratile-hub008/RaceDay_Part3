using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

// DTOs ("data transfer objects") are the shapes of the JSON that goes in and out of the API.
// The /// comments below are shown as descriptions on the Swagger page.

/// <summary>An error message returned with 400, 401, 403, 404, 409 and 503 responses.</summary>
/// <param name="Error">What went wrong, in plain English.</param>
public record ErrorDto(string Error);

// ---------- Users ----------

/// <summary>Details sent when a new user registers.</summary>
public class RegisterDto
{
    /// <summary>The user's first name.</summary>
    /// <example>Sipho</example>
    [Required, StringLength(100)] public string FirstName { get; set; } = "";

    /// <summary>The user's last name.</summary>
    /// <example>Dlamini</example>
    [Required, StringLength(100)] public string LastName { get; set; } = "";

    /// <summary>The email address used to log in. It must be unique.</summary>
    /// <example>sipho@example.com</example>
    [Required, EmailAddress, StringLength(256)] public string Email { get; set; } = "";

    /// <summary>The password. At least 8 characters.</summary>
    /// <example>Password123!</example>
    [Required, MinLength(8)] public string Password { get; set; } = "";

    /// <summary>The password again. It must match Password.</summary>
    /// <example>Password123!</example>
    [Required, Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = "";

    /// <summary>Either "Organiser" (creates events) or "Participant" (enters events).</summary>
    /// <example>Participant</example>
    [Required] public string Role { get; set; } = "";

    /// <summary>Optional phone number.</summary>
    /// <example>0821234567</example>
    public string? PhoneNumber { get; set; }

    /// <summary>Only needed when registering as an Organiser and the API has an organiser code set.</summary>
    public string? OrganiserCode { get; set; }
}

/// <summary>Details sent when a user logs in.</summary>
public class LoginDto
{
    /// <summary>The email address the user registered with.</summary>
    /// <example>thandi.organiser@raceday.co.za</example>
    [Required, EmailAddress] public string Email { get; set; } = "";

    /// <summary>The user's password.</summary>
    /// <example>Password123!</example>
    [Required] public string Password { get; set; } = "";

    /// <summary>True = stay logged in for 7 days. False = logged out after 30 minutes of no activity.</summary>
    public bool RememberMe { get; set; }
}

/// <summary>The profile details a user is allowed to change.</summary>
public class UpdateProfileDto
{
    /// <summary>The user's first name.</summary>
    /// <example>Sipho</example>
    [Required, StringLength(100)] public string FirstName { get; set; } = "";

    /// <summary>The user's last name.</summary>
    /// <example>Dlamini</example>
    [Required, StringLength(100)] public string LastName { get; set; } = "";

    /// <summary>Optional phone number.</summary>
    /// <example>0821234567</example>
    public string? PhoneNumber { get; set; }
}

/// <summary>A user as returned by the API (the password is never included).</summary>
/// <param name="Id">The user's Id number.</param>
/// <param name="FirstName">First name.</param>
/// <param name="LastName">Last name.</param>
/// <param name="Email">Email address.</param>
/// <param name="PhoneNumber">Phone number, if the user gave one.</param>
/// <param name="Role">"Organiser" or "Participant".</param>
/// <param name="ProfilePictureUrl">Link to the profile picture in Azure Blob Storage, if one was uploaded.</param>
public record UserDto(int Id, string FirstName, string LastName, string Email, string? PhoneNumber, string Role,
    string? ProfilePictureUrl);

// ---------- Events ----------

/// <summary>Details sent when an organiser creates or changes an event.</summary>
public class EventRequestDto
{
    /// <summary>The name of the event.</summary>
    /// <example>Soweto Community 10K</example>
    [Required, StringLength(150)] public string Name { get; set; } = "";

    /// <summary>A short description of the event.</summary>
    /// <example>A friendly community run through Soweto.</example>
    public string Description { get; set; } = "";

    /// <summary>The date and time the event starts.</summary>
    /// <example>2027-03-14T07:00:00</example>
    [Required] public DateTime EventDate { get; set; }

    /// <summary>Where the event takes place.</summary>
    /// <example>Soweto, Johannesburg</example>
    [Required, StringLength(200)] public string Location { get; set; } = "";

    /// <summary>The distance in kilometres (0.1 to 1000).</summary>
    /// <example>10</example>
    [Range(0.1, 1000)] public decimal DistanceKm { get; set; }

    /// <summary>Run, Walk or Cycle.</summary>
    [Required] public EventType EventType { get; set; }
}

/// <summary>An event as returned by the API.</summary>
/// <param name="Id">The event's Id number.</param>
/// <param name="Name">Event name.</param>
/// <param name="Description">Event description.</param>
/// <param name="EventDate">When the event starts.</param>
/// <param name="Location">Where the event takes place.</param>
/// <param name="DistanceKm">Distance in kilometres.</param>
/// <param name="EventType">Run, Walk or Cycle.</param>
/// <param name="OrganiserId">The Id of the organiser who created the event.</param>
/// <param name="OrganiserName">The organiser's full name.</param>
/// <param name="BannerUrl">Link to the banner image in Azure Blob Storage, if one was uploaded.</param>
public record EventDto(int Id, string Name, string Description, DateTime EventDate, string Location,
    decimal DistanceKm, EventType EventType, int OrganiserId, string OrganiserName, string? BannerUrl);

// ---------- Categories ----------

/// <summary>Details sent when an organiser creates or changes a category.</summary>
public class CategoryRequestDto
{
    /// <summary>The category name.</summary>
    /// <example>10km Open</example>
    [Required, StringLength(100)] public string Name { get; set; } = "";

    /// <summary>Optional description of who the category is for.</summary>
    /// <example>Open to all adults.</example>
    public string? Description { get; set; }
}

/// <summary>A category (such as "10km Open") that belongs to one event.</summary>
/// <param name="Id">The category's Id number.</param>
/// <param name="EventId">The Id of the event this category belongs to.</param>
/// <param name="Name">Category name.</param>
/// <param name="Description">Category description.</param>
public record CategoryDto(int Id, int EventId, string Name, string? Description);

// ---------- Enrolments ----------

/// <summary>Details sent when a participant enters an event.</summary>
public class EnrolRequestDto
{
    /// <summary>The Id of the category to enter. It must belong to the event.</summary>
    /// <example>1</example>
    [Range(1, int.MaxValue)] public int CategoryId { get; set; }
}

/// <summary>An enrolment: one participant entered into one event category.</summary>
/// <param name="Id">The enrolment's Id number.</param>
/// <param name="EventId">The Id of the event.</param>
/// <param name="EventName">The name of the event.</param>
/// <param name="CategoryId">The Id of the category.</param>
/// <param name="CategoryName">The name of the category.</param>
/// <param name="ParticipantId">The Id of the participant.</param>
/// <param name="ParticipantName">The participant's full name.</param>
/// <param name="EnrolledAt">When the participant entered.</param>
/// <param name="EventDate">When the event starts.</param>
/// <param name="Status">Pending (waiting for the organiser) or Confirmed.</param>
public record EnrolmentDto(int Id, int EventId, string EventName, int CategoryId, string CategoryName,
    int ParticipantId, string ParticipantName, DateTime EnrolledAt, DateTime EventDate,
    EnrolmentStatus Status);

/// <summary>The new status an organiser sets on an enrolment.</summary>
public class EnrolmentStatusDto
{
    /// <summary>Pending or Confirmed.</summary>
    [Required] public EnrolmentStatus Status { get; set; }
}

// ---------- Results ----------

/// <summary>Details sent when an organiser records or changes a result.</summary>
public class ResultRequestDto
{
    /// <summary>The finish time as hours:minutes:seconds.</summary>
    /// <example>00:52:30</example>
    [Required] public TimeSpan FinishTime { get; set; }

    /// <summary>The finishing position (1 = first).</summary>
    /// <example>1</example>
    [Range(1, int.MaxValue)] public int Position { get; set; }
}

/// <summary>A race result as returned by the API.</summary>
/// <param name="Id">The result's Id number.</param>
/// <param name="EnrolmentId">The Id of the enrolment this result belongs to.</param>
/// <param name="EventId">The Id of the event.</param>
/// <param name="EventName">The name of the event.</param>
/// <param name="ParticipantId">The Id of the participant.</param>
/// <param name="ParticipantName">The participant's full name.</param>
/// <param name="FinishTime">The finish time as text, for example 00:52:30.</param>
/// <param name="Position">The finishing position (1 = first).</param>
/// <param name="EventDate">When the event took place.</param>
/// <param name="CategoryName">The category the participant entered.</param>
/// <param name="TotalFinishers">How many participants have a result in this event.</param>
public record ResultDto(int Id, int EnrolmentId, int EventId, string EventName, int ParticipantId,
    string ParticipantName, string FinishTime, int Position, DateTime EventDate, string CategoryName,
    int TotalFinishers);
