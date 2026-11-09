using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using static RaceDay.Api.Tests.TestHelpers;

namespace RaceDay.Api.Tests;

public class UploadAndStatusTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public UploadAndStatusTests(ApiFactory factory) => _factory = factory;

    private static MultipartFormDataContent FileContent(string fileName, string contentType)
    {
        var file = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var form = new MultipartFormDataContent();
        form.Add(file, "file", fileName);
        return form;
    }

    // Test: Organiser - Can Upload Event Banner.
    [Fact]
    public async Task Organiser_CanUploadEventBanner()
    {
        var organiser = await LoggedInClient(_factory, "Organiser");
        var (eventId, _) = await CreateEventWithCategory(organiser);

        var response = await organiser.PostAsync($"/api/events/{eventId}/banner", FileContent("banner.png", "image/png"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("fake-storage", body.GetProperty("bannerUrl").GetString());
    }

    // Test: Upload Banner - Not An Image - Returns400.
    [Fact]
    public async Task UploadBanner_NotAnImage_Returns400()
    {
        var organiser = await LoggedInClient(_factory, "Organiser");
        var (eventId, _) = await CreateEventWithCategory(organiser);

        var response = await organiser.PostAsync($"/api/events/{eventId}/banner", FileContent("notes.txt", "text/plain"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Test: Participant - Cannot Upload Event Banner - Returns403.
    [Fact]
    public async Task Participant_CannotUploadEventBanner_Returns403()
    {
        var organiser = await LoggedInClient(_factory, "Organiser");
        var participant = await LoggedInClient(_factory, "Participant");
        var (eventId, _) = await CreateEventWithCategory(organiser);

        var response = await participant.PostAsync($"/api/events/{eventId}/banner", FileContent("banner.png", "image/png"));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // Test: User - Can Upload Profile Picture.
    [Fact]
    public async Task User_CanUploadProfilePicture()
    {
        var participant = await LoggedInClient(_factory, "Participant");

        var response = await participant.PostAsync("/api/users/me/picture", FileContent("me.jpg", "image/jpeg"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var me = await participant.GetFromJsonAsync<JsonElement>("/api/users/me");
        Assert.Contains("fake-storage", me.GetProperty("profilePictureUrl").GetString());
    }

    // Test: Enrolment - Starts Pending - And Organiser Can Confirm.
    [Fact]
    public async Task Enrolment_StartsPending_AndOrganiserCanConfirm()
    {
        var organiser = await LoggedInClient(_factory, "Organiser");
        var participant = await LoggedInClient(_factory, "Participant");
        var (eventId, categoryId) = await CreateEventWithCategory(organiser);

        var enrol = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        var created = await enrol.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Pending", created.GetProperty("status").GetString());

        int enrolmentId = created.GetProperty("id").GetInt32();
        var confirm = await organiser.PutAsJsonAsync($"/api/enrolments/{enrolmentId}/status", new { status = "Confirmed" });
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);

        var mine = await participant.GetFromJsonAsync<JsonElement>("/api/enrolments/mine");
        Assert.Equal("Confirmed", mine[0].GetProperty("status").GetString());
    }

    // Test: Participant - Cannot Change Enrolment Status - Returns403.
    [Fact]
    public async Task Participant_CannotChangeEnrolmentStatus_Returns403()
    {
        var organiser = await LoggedInClient(_factory, "Organiser");
        var participant = await LoggedInClient(_factory, "Participant");
        var (eventId, categoryId) = await CreateEventWithCategory(organiser);

        var enrol = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        int enrolmentId = (await enrol.Content.ReadFromJsonAsync<IdDto>())!.Id;

        var response = await participant.PutAsJsonAsync($"/api/enrolments/{enrolmentId}/status", new { status = "Confirmed" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // Test: My Results - Shows Position And Total Finishers.
    [Fact]
    public async Task MyResults_ShowsPositionAndTotalFinishers()
    {
        var organiser = await LoggedInClient(_factory, "Organiser");
        var participant = await LoggedInClient(_factory, "Participant");
        var (eventId, categoryId) = await CreateEventWithCategory(organiser);

        var enrol = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        int enrolmentId = (await enrol.Content.ReadFromJsonAsync<IdDto>())!.Id;
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", new { finishTime = "00:45:10", position = 1 });

        var results = await participant.GetFromJsonAsync<JsonElement>("/api/results/mine");
        Assert.Equal(1, results[0].GetProperty("position").GetInt32());
        Assert.Equal(1, results[0].GetProperty("totalFinishers").GetInt32());
        Assert.Equal("10km Open", results[0].GetProperty("categoryName").GetString());
    }
}
