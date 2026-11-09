using System.Net.Http.Json;
using static RaceDay.Api.Tests.TestHelpers;

namespace RaceDay.Api.Tests;

// Tests for the optional filters on GET /api/events and GET /api/events/{id}/enrolments.
public class FilterTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public FilterTests(ApiFactory factory) => _factory = factory;

    // Only the fields these tests need.
    private record EventRow(int Id, string Name, decimal DistanceKm);
    private record EnrolmentRow(int Id, string Status);

    // Creates an event with the given name, date and distance.
    private static async Task<int> CreateEvent(HttpClient organiser, string name, int daysAhead, decimal distanceKm)
    {
        var response = await organiser.PostAsJsonAsync("/api/events", new
        {
            name, description = "Test", eventDate = DateTime.UtcNow.AddDays(daysAhead),
            location = "Pretoria", distanceKm, eventType = "Run"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IdDto>())!.Id;
    }

    // Test: Search - Finds The Event By Name.
    [Fact]
    public async Task Search_FindsTheEventByName()
    {
        var organiser = await LoggedInClient(_factory, "Organiser");
        string unique = "Searchable" + Guid.NewGuid().ToString("N")[..8];
        int id = await CreateEvent(organiser, unique, 20, 10);
        await CreateEvent(organiser, "Something else", 20, 10);

        var found = await organiser.GetFromJsonAsync<List<EventRow>>($"/api/events?search={unique}");

        Assert.Single(found!);
        Assert.Equal(id, found![0].Id);
    }

    // Test: Upcoming Only - Hides Past Events.
    [Fact]
    public async Task UpcomingOnly_HidesPastEvents()
    {
        var organiser = await LoggedInClient(_factory, "Organiser");
        string unique = "Timing" + Guid.NewGuid().ToString("N")[..8];
        await CreateEvent(organiser, unique + " past", -10, 10);
        int futureId = await CreateEvent(organiser, unique + " future", 10, 10);

        var all = await organiser.GetFromJsonAsync<List<EventRow>>($"/api/events?search={unique}");
        var upcoming = await organiser.GetFromJsonAsync<List<EventRow>>($"/api/events?search={unique}&upcomingOnly=true");

        Assert.Equal(2, all!.Count);
        Assert.Single(upcoming!);
        Assert.Equal(futureId, upcoming![0].Id);
    }

    // Test: Distance Filters - Keep Only Events In Range.
    [Fact]
    public async Task DistanceFilters_KeepOnlyEventsInRange()
    {
        var organiser = await LoggedInClient(_factory, "Organiser");
        string unique = "Distance" + Guid.NewGuid().ToString("N")[..8];
        await CreateEvent(organiser, unique + " short", 10, 5);
        await CreateEvent(organiser, unique + " long", 10, 42);

        var shortOnes = await organiser.GetFromJsonAsync<List<EventRow>>($"/api/events?search={unique}&maxDistanceKm=10");
        var longOnes = await organiser.GetFromJsonAsync<List<EventRow>>($"/api/events?search={unique}&minDistanceKm=21");

        Assert.Single(shortOnes!);
        Assert.Equal(5, shortOnes![0].DistanceKm);
        Assert.Single(longOnes!);
        Assert.Equal(42, longOnes![0].DistanceKm);
    }

    // Test: Enrolment Status Filter - Separates Pending From Confirmed.
    [Fact]
    public async Task EnrolmentStatusFilter_SeparatesPendingFromConfirmed()
    {
        var organiser = await LoggedInClient(_factory, "Organiser");
        var participant = await LoggedInClient(_factory, "Participant");
        var (eventId, categoryId) = await CreateEventWithCategory(organiser);

        var enrol = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        enrol.EnsureSuccessStatusCode();
        int enrolmentId = (await enrol.Content.ReadFromJsonAsync<IdDto>())!.Id;

        var pending = await organiser.GetFromJsonAsync<List<EnrolmentRow>>($"/api/events/{eventId}/enrolments?status=Pending");
        var confirmedBefore = await organiser.GetFromJsonAsync<List<EnrolmentRow>>($"/api/events/{eventId}/enrolments?status=Confirmed");
        Assert.Single(pending!);
        Assert.Empty(confirmedBefore!);

        var confirm = await organiser.PutAsJsonAsync($"/api/enrolments/{enrolmentId}/status", new { status = "Confirmed" });
        confirm.EnsureSuccessStatusCode();

        var confirmedAfter = await organiser.GetFromJsonAsync<List<EnrolmentRow>>($"/api/events/{eventId}/enrolments?status=Confirmed");
        Assert.Single(confirmedAfter!);
    }
}
