using Microsoft.AspNetCore.Identity;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

// Adds demo users, events, categories and enrolments the first time the API runs.
public static class DbSeeder
{
    public const string DemoPassword = "Password123!";

    public static void Seed(AppDbContext db, IPasswordHasher<AppUser> hasher)
    {
        // Only seed an empty database.
        if (db.Users.Any()) return;

        AppUser Make(string first, string last, string email, int roleId)
        {
            var u = new AppUser { FirstName = first, LastName = last, Email = email, RoleId = roleId };
            u.PasswordHash = hasher.HashPassword(u, DemoPassword);
            return u;
        }

        // Demo users (all use the password above).
        var org1 = Make("Thandi", "Nkosi", "thandi.organiser@raceday.co.za", Roles.OrganiserId);
        var org2 = Make("Pieter", "van Wyk", "pieter.organiser@raceday.co.za", Roles.OrganiserId);
        var par1 = Make("Sipho", "Dlamini", "sipho.participant@raceday.co.za", Roles.ParticipantId);
        var par2 = Make("Aisha", "Patel", "aisha.participant@raceday.co.za", Roles.ParticipantId);

        // Demo events.
        var soweto = new Event
        {
            Name = "Soweto Community 10K", Description = "A friendly community run through Soweto.",
            EventDate = DateTime.UtcNow.AddDays(30), Location = "Soweto, Johannesburg",
            DistanceKm = 10, EventType = EventType.Run, Organiser = org1
        };
        var tour = new Event
        {
            Name = "Cape Town Charity Cycle", Description = "Scenic charity cycle around the peninsula.",
            EventDate = DateTime.UtcNow.AddDays(45), Location = "Cape Town",
            DistanceKm = 50, EventType = EventType.Cycle, Organiser = org2
        };
        var walk = new Event
        {
            Name = "Durban Beachfront Walk", Description = "Relaxed 5km walk along the beachfront.",
            EventDate = DateTime.UtcNow.AddDays(60), Location = "Durban",
            DistanceKm = 5, EventType = EventType.Walk, Organiser = org1
        };

        // Demo categories.
        var c10 = new Category { Name = "10km Open", Event = soweto };
        var cU20 = new Category { Name = "Under 20", Event = soweto };
        var c50 = new Category { Name = "50km Senior", Event = tour };
        var c25 = new Category { Name = "25km Social", Event = tour };
        var c5 = new Category { Name = "5km Walk", Event = walk };
        var cFam = new Category { Name = "Family", Event = walk };

        // Save everything. EF works out the links between the rows.
        db.Users.AddRange(org1, org2, par1, par2);
        db.Events.AddRange(soweto, tour, walk);
        db.Categories.AddRange(c10, cU20, c50, c25, c5, cFam);
        db.Enrolments.AddRange(
            new Enrolment { Event = soweto, Category = c10, Participant = par1, Status = EnrolmentStatus.Confirmed },
            new Enrolment { Event = soweto, Category = cU20, Participant = par2 },
            new Enrolment { Event = tour, Category = c25, Participant = par1, Status = EnrolmentStatus.Confirmed });
        db.SaveChanges();
    }
}
