using System.Net;
using System.Net.Http.Json;
using static RaceDay.Api.Tests.TestHelpers;

namespace RaceDay.Api.Tests;

public class EventTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public EventTests(ApiFactory factory) => _factory = factory;

    // Test: Organiser - Can Create Event - Returns201.
    [Fact]
    public async Task Organiser_CanCreateEvent_Returns201()
    {
        var organiser = await LoggedInClient(_factory, "Organiser");
        var response = await organiser.PostAsJsonAsync("/api/events", EventBody());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // Test: Organiser - Create Event - Invalid Body - Returns400.
    [Fact]
    public async Task Organiser_CreateEvent_InvalidBody_Returns400()
    {
        var organiser = await LoggedInClient(_factory, "Organiser");
        var response = await organiser.PostAsJsonAsync("/api/events",
            new { name = "", location = "", distanceKm = 0, eventType = "Run", eventDate = DateTime.UtcNow.AddDays(5) });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Test: Participant - Cannot Create Event - Returns403.
    [Fact]
    public async Task Participant_CannotCreateEvent_Returns403()
    {
        var participant = await LoggedInClient(_factory, "Participant");
        var response = await participant.PostAsJsonAsync("/api/events", EventBody());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // Test: Both Roles - Can View Events.
    [Fact]
    public async Task BothRoles_CanViewEvents()
    {
        var organiser = await LoggedInClient(_factory, "Organiser");
        var participant = await LoggedInClient(_factory, "Participant");
        var (eventId, _) = await CreateEventWithCategory(organiser);

        Assert.Equal(HttpStatusCode.OK, (await organiser.GetAsync($"/api/events/{eventId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await participant.GetAsync($"/api/events/{eventId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await participant.GetAsync("/api/events")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await participant.GetAsync($"/api/events/{eventId}/categories")).StatusCode);
    }

    // Test: Get Event - Not Found - Returns404.
    [Fact]
    public async Task GetEvent_NotFound_Returns404()
    {
        var participant = await LoggedInClient(_factory, "Participant");
        Assert.Equal(HttpStatusCode.NotFound, (await participant.GetAsync("/api/events/999999")).StatusCode);
    }

    // Test: Organiser - Can Update And Delete Own Event.
    [Fact]
    public async Task Organiser_CanUpdateAndDeleteOwnEvent()
    {
        var organiser = await LoggedInClient(_factory, "Organiser");
        var (eventId, _) = await CreateEventWithCategory(organiser);

        var update = await organiser.PutAsJsonAsync($"/api/events/{eventId}", EventBody("Renamed 10K", 40));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var delete = await organiser.DeleteAsync($"/api/events/{eventId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await organiser.GetAsync($"/api/events/{eventId}")).StatusCode);
    }

    // Test: Organiser - Cannot Modify Another Organisers Event - Returns403.
    [Fact]
    public async Task Organiser_CannotModifyAnotherOrganisersEvent_Returns403()
    {
        var owner = await LoggedInClient(_factory, "Organiser");
        var other = await LoggedInClient(_factory, "Organiser");
        var (eventId, _) = await CreateEventWithCategory(owner);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await other.PutAsJsonAsync($"/api/events/{eventId}", EventBody("Hijack"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.DeleteAsync($"/api/events/{eventId}")).StatusCode);
    }

    // Test: Participant - Cannot Update Or Delete Event - Returns403.
    [Fact]
    public async Task Participant_CannotUpdateOrDeleteEvent_Returns403()
    {
        var organiser = await LoggedInClient(_factory, "Organiser");
        var participant = await LoggedInClient(_factory, "Participant");
        var (eventId, _) = await CreateEventWithCategory(organiser);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await participant.PutAsJsonAsync($"/api/events/{eventId}", EventBody())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await participant.DeleteAsync($"/api/events/{eventId}")).StatusCode);
    }

    // Test: Participant - Cannot Create Category - Returns403.
    [Fact]
    public async Task Participant_CannotCreateCategory_Returns403()
    {
        var organiser = await LoggedInClient(_factory, "Organiser");
        var participant = await LoggedInClient(_factory, "Participant");
        var (eventId, _) = await CreateEventWithCategory(organiser);

        var response = await participant.PostAsJsonAsync($"/api/events/{eventId}/categories", new { name = "Sneaky" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
