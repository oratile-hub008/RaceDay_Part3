using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace RaceDay.Api.Auth;

// The names of the values stored in the session.
public static class SessionAuth
{
    public const string Scheme = "Session";
    public const string UserId = "UserId";
    public const string Role = "Role";
    public const string Name = "Name";
    public const string RememberMe = "RememberMe";
    public const string LastSeen = "LastSeen";

    // The session itself lasts 7 days (see Program.cs). Users who did not tick
    // "remember me" are logged out after this much inactivity.
    public static readonly TimeSpan NormalTimeout = TimeSpan.FromMinutes(30);
}

// Our own login check. On every request it looks in the session, and if a user is stored
// there it builds the "claims" (Id, name, role) that [Authorize] uses.
public class SessionAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public SessionAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // 1. Read who is logged in from the session.
        int? id = Context.Session.GetInt32(SessionAuth.UserId);
        string? role = Context.Session.GetString(SessionAuth.Role);
        string name = Context.Session.GetString(SessionAuth.Name) ?? "";

        if (id == null || string.IsNullOrEmpty(role))
            return Task.FromResult(AuthenticateResult.NoResult());

        // 2. Users who did not tick "remember me" are logged out after 30 minutes of inactivity.
        if (Context.Session.GetInt32(SessionAuth.RememberMe) != 1)
        {
            var lastSeen = long.Parse(Context.Session.GetString(SessionAuth.LastSeen) ?? "0");
            if (DateTime.UtcNow - new DateTime(lastSeen, DateTimeKind.Utc) > SessionAuth.NormalTimeout)
            {
                Context.Session.Clear();
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            Context.Session.SetString(SessionAuth.LastSeen, DateTime.UtcNow.Ticks.ToString());
        }

        // 3. Build the user's identity for this request.
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, id.Value.ToString()),
            new Claim(ClaimTypes.Name, name),
            new Claim(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, SessionAuth.Scheme);
        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SessionAuth.Scheme)));
    }

    // Not logged in: answer 401 instead of redirecting to a login page (this is an API).
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        return Task.CompletedTask;
    }

    // Logged in but the wrong role: answer 403.
    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 403;
        return Task.CompletedTask;
    }
}
