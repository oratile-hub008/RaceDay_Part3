using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using static RaceDay.Api.Tests.TestHelpers;

namespace RaceDay.Api.Tests;

public class EnrolmentAndResultTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public EnrolmentAndResultTests(ApiFactory factory) => _factory = factory;

    // Test: Participant - Can Enrol - And Organiser Sees Enrolment.
    [Fact]
    public async Task Participant_CanEnrol_AndOrganiserSeesEnrolment()
    {
        var organiser = await LoggedInClient(_factory, "Organiser");
        var participant = await LoggedInClient(_factory, "Participant");
        var (eventId, categoryId) = await CreateEventWithCategory(organiser);

        var enrol = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        Assert.Equal(HttpStatusCode.Created, enrol.StatusCode);

        var mine = await participant.GetFromJsonAsync<JsonElement>("/api/enrolments/mine");
        Assert.Equal(1, mine.GetArrayLength());
        Assert.Equal(categoryId, mine[0].GetProperty("categoryId").GetInt32());

        var all = await organiser.GetFromJsonAsync<JsonElement>($"/api/events/{eventId}/enrolments");
        Assert.Equal(1, all.GetArrayLength());
    }

    // Test: Participant - Enrol Twice - Returns409.
    [Fact]
    public async Task Participant_EnrolTwice_Returns409()
    {
        var organiser = await LoggedInClient(_factory, "Organiser");
        var participant = await LoggedInClient(_factory, "Participant");
        var (eventId, categoryId) = await CreateEventWithCategory(organiser);

        await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        var second = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    // Test: Participant - Enrol With Category From Another Event - Returns400.
    [Fact]
    public async Task Participant_EnrolWithCategoryFromAnotherEvent_Returns400()
    {
        var organiser = await LoggedInClient(_factory, "Organiser");
        var participant = await LoggedInClient(_factory, "Participant");
        var (eventA, _) = await CreateEventWithCategory(organiser);
        var (_, categoryB) = await CreateEventWithCategory(organiser);

        var response = await participant.PostAsJsonAsync($"/api/events/{eventA}/enrolments", new { categoryId = categoryB });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Test: Enrol - Unknown Event - Returns404.
    [Fact]
    public async Task Enrol_UnknownEvent_Returns404()
    {
        var participant = await LoggedInClient(_factory, "Participant");
        var response = await participant.PostAsJsonAsync("/api/events/999999/enrolments", new { categoryId = 1 });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Test: Organiser - Cannot Enrol - Returns403.
    [Fact]
    public async Task Organiser_CannotEnrol_Returns403()
    {
        var organiser = await LoggedInClient(_factory, "Organiser");
        var (eventId, categoryId) = await CreateEventWithCategory(organiser);

        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // Test: Participant - Cannot View Event Enrolment List - Returns403.
    [Fact]
    public async Task Participant_CannotViewEventEnrolmentList_Returns403()
    {
        var organiser = await LoggedInClient(_factory, "Organiser");
        var participant = await LoggedInClient(_factory, "Participant");
        var (eventId, _) = await CreateEventWithCategory(organiser);

        Assert.Equal(HttpStatusCode.Forbidden, (await participant.GetAsync($"/api/events/{eventId}/enrolments")).StatusCode);
    }

    // Test: Other Organiser - Cannot View Enrolments - Returns403.
    [Fact]
    public async Task OtherOrganiser_CannotViewEnrolments_Returns403()
    {
        var owner = await LoggedInClient(_factory, "Organiser");
        var other = await LoggedInClient(_factory, "Organiser");
        var (eventId, _) = await CreateEventWithCategory(owner);

        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"/api/events/{eventId}/enrolments")).StatusCode);
    }

    // Test: Organiser - Can Record Result - And Participant Sees Own Result.
    [Fact]
    public async Task Organiser_CanRecordResult_AndParticipantSeesOwnResult()
    {
        var organiser = await LoggedInClient(_factory, "Organiser");
        var participant = await LoggedInClient(_factory, "Participant");
        var (eventId, categoryId) = await CreateEventWithCategory(organiser);

        var enrol = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        var enrolmentId = (await enrol.Content.ReadFromJsonAsync<IdDto>())!.Id;

        var record = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result",
            new { finishTime = "00:52:31", position = 1 });
        Assert.Equal(HttpStatusCode.Created, record.StatusCode);

        var results = await participant.GetFromJsonAsync<JsonElement>("/api/results/mine");
        Assert.Equal(1, results.GetArrayLength());
        Assert.Equal("00:52:31", results[0].GetProperty("finishTime").GetString());
        Assert.Equal(1, results[0].GetProperty("position").GetInt32());

        var eventResults = await organiser.GetFromJsonAsync<JsonElement>($"/api/events/{eventId}/results");
        Assert.Equal(1, eventResults.GetArrayLength());
    }

    // Test: Organiser - Record Result Twice - Returns409.
    [Fact]
    public async Task Organiser_RecordResultTwice_Returns409()
    {
        var organiser = await LoggedInClient(_factory, "Organiser");
        var participant = await LoggedInClient(_factory, "Participant");
        var (eventId, categoryId) = await CreateEventWithCategory(organiser);
        var enrol = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        var enrolmentId = (await enrol.Content.ReadFromJsonAsync<IdDto>())!.Id;

        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", new { finishTime = "00:50:00", position = 1 });
        var second = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", new { finishTime = "00:51:00", position = 2 });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    // Test: Participant - Cannot Record Result - Returns403.
    [Fact]
    public async Task Participant_CannotRecordResult_Returns403()
    {
        var organiser = await LoggedInClient(_factory, "Organiser");
        var participant = await LoggedInClient(_factory, "Participant");
        var (eventId, categoryId) = await CreateEventWithCategory(organiser);
        var enrol = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        var enrolmentId = (await enrol.Content.ReadFromJsonAsync<IdDto>())!.Id;

        var response = await participant.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result",
            new { finishTime = "00:40:00", position = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // Test: Other Organiser - Cannot Record Result - Returns403.
    [Fact]
    public async Task OtherOrganiser_CannotRecordResult_Returns403()
    {
        var owner = await LoggedInClient(_factory, "Organiser");
        var other = await LoggedInClient(_factory, "Organiser");
        var participant = await LoggedInClient(_factory, "Participant");
        var (eventId, categoryId) = await CreateEventWithCategory(owner);
        var enrol = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        var enrolmentId = (await enrol.Content.ReadFromJsonAsync<IdDto>())!.Id;

        var response = await other.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result",
            new { finishTime = "00:40:00", position = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // Test: Organiser - Cannot View Participant Only Results Endpoint - Returns403.
    [Fact]
    public async Task Organiser_CannotViewParticipantOnlyResultsEndpoint_Returns403()
    {
        var organiser = await LoggedInClient(_factory, "Organiser");
        Assert.Equal(HttpStatusCode.Forbidden, (await organiser.GetAsync("/api/results/mine")).StatusCode);
    }
}
