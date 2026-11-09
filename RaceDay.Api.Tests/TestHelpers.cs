using System.Net.Http.Json;

namespace RaceDay.Api.Tests;

public record IdDto(int Id);

public static class TestHelpers
{
    public const string Password = "Password123!";

    public static string NewEmail(string prefix) => $"{prefix}-{Guid.NewGuid():N}@test.co.za";

    public static object RegisterBody(string email, string role) => new
    {
        firstName = "Test", lastName = "User", email, password = Password, confirmPassword = Password, role
    };

    /// <summary>Registers a user and returns a client that is logged in as them.</summary>
    public static async Task<HttpClient> LoggedInClient(ApiFactory factory, string role, string? email = null)
    {
        var client = factory.CreateClient();
        email ??= NewEmail(role.ToLower());
        var reg = await client.PostAsJsonAsync("/api/auth/register", RegisterBody(email, role));
        reg.EnsureSuccessStatusCode();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        login.EnsureSuccessStatusCode();
        return client;
    }

    public static object EventBody(string name = "Test 10K", int daysAhead = 30) => new
    {
        name, description = "Test event", eventDate = DateTime.UtcNow.AddDays(daysAhead),
        location = "Johannesburg", distanceKm = 10, eventType = "Run"
    };

    /// <summary>Organiser creates an event with one category. Returns (eventId, categoryId).</summary>
    public static async Task<(int EventId, int CategoryId)> CreateEventWithCategory(HttpClient organiser)
    {
        var ev = await organiser.PostAsJsonAsync("/api/events", EventBody());
        ev.EnsureSuccessStatusCode();
        var eventId = (await ev.Content.ReadFromJsonAsync<IdDto>())!.Id;

        var cat = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new { name = "10km Open" });
        cat.EnsureSuccessStatusCode();
        var categoryId = (await cat.Content.ReadFromJsonAsync<IdDto>())!.Id;
        return (eventId, categoryId);
    }
}
