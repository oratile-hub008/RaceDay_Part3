using System.Globalization;

namespace RaceDay.Web.Helpers;

/// <summary>Small helpers that keep the Razor views tidy and consistent.</summary>
public static class UiHelper
{
    public static string Date(DateTime date) => date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);

    public static string DateAndTime(DateTime date) => date.ToString("dd MMM yyyy, HH:mm", CultureInfo.InvariantCulture);

    public static string Distance(decimal km) => km.ToString("0.##", CultureInfo.InvariantCulture) + " km";

    public static string TypeIcon(string type) => type switch
    {
        "Run" => "bi-lightning-charge-fill",
        "Walk" => "bi-person-walking",
        "Cycle" => "bi-bicycle",
        _ => "bi-flag-fill"
    };

    public static string Initials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1 ? parts[0][..1].ToUpper() : (parts[0][..1] + parts[^1][..1]).ToUpper();
    }

    public static string DaysUntil(DateTime date)
    {
        int days = (int)Math.Ceiling((date - System.DateTime.UtcNow).TotalDays);
        if (days < 0) return "Completed";
        if (days == 0) return "Today";
        return days == 1 ? "Tomorrow" : $"In {days} days";
    }
}
