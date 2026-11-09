using System.Net;
using System.Net.Http.Json;
using static RaceDay.Api.Tests.TestHelpers;

namespace RaceDay.Api.Tests;

public class AuthTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public AuthTests(ApiFactory factory) => _factory = factory;

    // Test: Register - Valid User - Returns201.
    [Theory]
    [InlineData("Organiser")]
    [InlineData("Participant")]
    public async Task Register_ValidUser_Returns201(string role)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", RegisterBody(NewEmail("reg"), role));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // Test: Register - Duplicate Email - Returns409.
    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var client = _factory.CreateClient();
        var email = NewEmail("dup");
        await client.PostAsJsonAsync("/api/auth/register", RegisterBody(email, "Participant"));
        var second = await client.PostAsJsonAsync("/api/auth/register", RegisterBody(email, "Participant"));
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    // Test: Register - Invalid Role - Returns400.
    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", RegisterBody(NewEmail("bad"), "Admin"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Test: Register - Short Password - Returns400.
    [Fact]
    public async Task Register_ShortPassword_Returns400()
    {
        var client = _factory.CreateClient();
        var body = new { firstName = "A", lastName = "B", email = NewEmail("short"), password = "123", confirmPassword = "123", role = "Participant" };
        var response = await client.PostAsJsonAsync("/api/auth/register", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Test: Register - Passwords Do Not Match - Returns400.
    [Fact]
    public async Task Register_PasswordsDoNotMatch_Returns400()
    {
        var client = _factory.CreateClient();
        var body = new { firstName = "A", lastName = "B", email = NewEmail("mismatch"), password = Password,
            confirmPassword = "Different123!", role = "Participant" };
        var response = await client.PostAsJsonAsync("/api/auth/register", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Test: Login - With Remember Me - Returns200.
    [Fact]
    public async Task Login_WithRememberMe_Returns200()
    {
        var client = _factory.CreateClient();
        var email = NewEmail("remember");
        await client.PostAsJsonAsync("/api/auth/register", RegisterBody(email, "Participant"));
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password, rememberMe = true });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/users/me")).StatusCode);
    }

    // Test: Login - Valid Credentials - Returns200.
    [Fact]
    public async Task Login_ValidCredentials_Returns200()
    {
        var client = _factory.CreateClient();
        var email = NewEmail("login");
        await client.PostAsJsonAsync("/api/auth/register", RegisterBody(email, "Participant"));
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Test: Login - Wrong Password - Returns401.
    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var client = _factory.CreateClient();
        var email = NewEmail("wrong");
        await client.PostAsJsonAsync("/api/auth/register", RegisterBody(email, "Participant"));
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "NotThePassword1" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Test: Login - Unknown User - Returns401.
    [Fact]
    public async Task Login_UnknownUser_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = NewEmail("ghost"), password = Password });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Test: Protected Endpoints - Without Login - Return401.
    [Theory]
    [InlineData("/api/events")]
    [InlineData("/api/users/me")]
    [InlineData("/api/enrolments/mine")]
    [InlineData("/api/results/mine")]
    public async Task ProtectedEndpoints_WithoutLogin_Return401(string url)
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Test: Logout - Ends Session.
    [Fact]
    public async Task Logout_EndsSession()
    {
        var client = await LoggedInClient(_factory, "Participant");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/users/me")).StatusCode);

        var logout = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);
    }

    // Test: Profile - Can Be Viewed And Updated.
    [Fact]
    public async Task Profile_CanBeViewedAndUpdated()
    {
        var client = await LoggedInClient(_factory, "Participant");
        var update = await client.PutAsJsonAsync("/api/users/me",
            new { firstName = "Updated", lastName = "Name", phoneNumber = "0821234567" });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var me = await client.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/users/me");
        Assert.Equal("Updated", me.GetProperty("firstName").GetString());
    }
}
