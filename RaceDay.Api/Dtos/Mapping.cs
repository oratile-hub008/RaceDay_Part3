using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

// Mapping turns a database entity into a DTO (the JSON the API sends back).
// Keeping it here keeps the controllers small and easy to read.
public static class Mapping
{
    // "Sipho Dlamini"
    public static string FullName(AppUser? user) =>
        user == null ? "" : $"{user.FirstName} {user.LastName}";

    public static UserDto ToDto(AppUser u) =>
        new(u.Id, u.FirstName, u.LastName, u.Email, u.PhoneNumber, u.Role?.Name ?? "",
            u.ProfilePictureUrl);

    public static EventDto ToDto(Event e) =>
        new(e.Id, e.Name, e.Description, e.EventDate, e.Location, e.DistanceKm,
            e.EventType, e.OrganiserId, FullName(e.Organiser), e.BannerUrl);

    public static CategoryDto ToDto(Category c) =>
        new(c.Id, c.EventId, c.Name, c.Description);

    public static EnrolmentDto ToDto(Enrolment e) =>
        new(e.Id, e.EventId, e.Event?.Name ?? "", e.CategoryId, e.Category?.Name ?? "",
            e.ParticipantId, FullName(e.Participant), e.EnrolledAt, e.Event?.EventDate ?? default, e.Status);

    public static ResultDto ToDto(Result r) =>
        new(r.Id, r.EnrolmentId, r.Enrolment?.EventId ?? 0, r.Enrolment?.Event?.Name ?? "",
            r.Enrolment?.ParticipantId ?? 0, FullName(r.Enrolment?.Participant),
            r.FinishTime.ToString(@"hh\:mm\:ss"), r.Position, r.Enrolment?.Event?.EventDate ?? default,
            r.Enrolment?.Category?.Name ?? "", 0);
}
