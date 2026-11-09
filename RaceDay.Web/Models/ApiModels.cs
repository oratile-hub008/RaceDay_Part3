namespace RaceDay.Web.Models;

// These records match the JSON the API returns. Add a property here if the API adds a field.

public record UserInfo(int Id, string FirstName, string LastName, string Email, string? PhoneNumber,
    string Role, string? ProfilePictureUrl);

public record EventInfo(int Id, string Name, string Description, DateTime EventDate, string Location,
    decimal DistanceKm, string EventType, int OrganiserId, string OrganiserName, string? BannerUrl);

public record CategoryInfo(int Id, int EventId, string Name, string? Description);

public record EnrolmentInfo(int Id, int EventId, string EventName, int CategoryId, string CategoryName,
    int ParticipantId, string ParticipantName, DateTime EnrolledAt, DateTime EventDate, string Status);

public record ResultInfo(int Id, int EnrolmentId, int EventId, string EventName, int ParticipantId,
    string ParticipantName, string FinishTime, int Position, DateTime EventDate, string CategoryName,
    int TotalFinishers);
