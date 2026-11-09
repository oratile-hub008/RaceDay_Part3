using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

// The link between our C# classes and the SQL Server database.
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // One DbSet per table.
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        // The two roles are always in the database.
        model.Entity<Role>().HasData(
            new Role { Id = Models.Roles.OrganiserId, Name = Models.Roles.Organiser },
            new Role { Id = Models.Roles.ParticipantId, Name = Models.Roles.Participant });

        // Unique indexes stop duplicates (same role name, same email).
        model.Entity<Role>().HasIndex(r => r.Name).IsUnique();
        model.Entity<AppUser>().HasIndex(u => u.Email).IsUnique();

        model.Entity<AppUser>().HasOne(u => u.Role).WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId).OnDelete(DeleteBehavior.Restrict);

        // Enums are saved as text ("Run") so the data is easy to read in SQL.
        // Restrict = a row cannot be deleted while other rows still point to it.
        model.Entity<Event>().Property(e => e.EventType).HasConversion<string>();
        model.Entity<Event>().HasOne(e => e.Organiser).WithMany(u => u.OrganisedEvents)
            .HasForeignKey(e => e.OrganiserId).OnDelete(DeleteBehavior.Restrict);

        model.Entity<Enrolment>().Property(e => e.Status).HasConversion<string>();

        model.Entity<Category>().HasOne(c => c.Event).WithMany(e => e.Categories)
            .HasForeignKey(c => c.EventId).OnDelete(DeleteBehavior.Cascade);

        // A participant can only enter an event once.
        model.Entity<Enrolment>().HasIndex(e => new { e.EventId, e.ParticipantId }).IsUnique();
        model.Entity<Enrolment>().HasOne(e => e.Event).WithMany(x => x.Enrolments)
            .HasForeignKey(e => e.EventId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<Enrolment>().HasOne(e => e.Category).WithMany(c => c.Enrolments)
            .HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Enrolment>().HasOne(e => e.Participant).WithMany(u => u.Enrolments)
            .HasForeignKey(e => e.ParticipantId).OnDelete(DeleteBehavior.Restrict);

        // One result per enrolment. Deleting an enrolment deletes its result.
        model.Entity<Result>().HasIndex(r => r.EnrolmentId).IsUnique();
        model.Entity<Result>().HasOne(r => r.Enrolment).WithOne(e => e.Result)
            .HasForeignKey<Result>(r => r.EnrolmentId).OnDelete(DeleteBehavior.Cascade);
    }
}
