namespace RaceDay.Web.Helpers;

// The names of the values kept in the web session.
public static class SessionKeys
{
    public const string UserId = "UserId";
    public const string Role = "Role";
    public const string Name = "Name";
    public const string Picture = "Picture";
    public const string ApiCookie = "ApiCookie";
}

// The two role names (they must match the API).
public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}
