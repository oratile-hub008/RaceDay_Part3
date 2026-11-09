namespace RaceDay.Api.Models;

// The two kinds of user. Organisers create events; participants enter them.
public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";

    public const int OrganiserId = 1;
    public const int ParticipantId = 2;

    public static bool IsValid(string role) => role == Organiser || role == Participant;
}

// What kind of event it is.
public enum EventType { Run, Walk, Cycle }

// A new enrolment is Pending until the organiser confirms it.
public enum EnrolmentStatus { Pending, Confirmed }

// ---------- Database tables (one class = one table) ----------

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<AppUser> Users { get; set; } = new();
}

// A person who can log in.
public class AppUser
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int RoleId { get; set; }
    public Role? Role { get; set; }

    public List<Event> OrganisedEvents { get; set; } = new();
    public List<Enrolment> Enrolments { get; set; } = new();
}

// A race, walk or cycle created by an organiser.
public class Event
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public EventType EventType { get; set; }
    public string? BannerUrl { get; set; }

    public int OrganiserId { get; set; }
    public AppUser? Organiser { get; set; }

    public List<Category> Categories { get; set; } = new();
    public List<Enrolment> Enrolments { get; set; } = new();
}

// A group inside an event, for example "10km Open" or "Under 20".
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int EventId { get; set; }
    public Event? Event { get; set; }
    public List<Enrolment> Enrolments { get; set; } = new();
}

// One participant entered into one event in one category.
public class Enrolment
{
    public int Id { get; set; }
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public EnrolmentStatus Status { get; set; } = EnrolmentStatus.Pending;
    public int EventId { get; set; }
    public Event? Event { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public int ParticipantId { get; set; }
    public AppUser? Participant { get; set; }
    public Result? Result { get; set; }
}

// The finish time and position of an enrolment (one result per enrolment).
public class Result
{
    public int Id { get; set; }
    public TimeSpan FinishTime { get; set; }
    public int Position { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    public int EnrolmentId { get; set; }
    public Enrolment? Enrolment { get; set; }
}
