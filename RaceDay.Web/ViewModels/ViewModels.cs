using System.ComponentModel.DataAnnotations;
using RaceDay.Web.Models;
namespace RaceDay.Web.ViewModels;

// ---------- Account ----------

// Data and validation rules for the login page.
public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email address."), EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Please enter your password."), DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

// Data and validation rules for the register page.
public class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter your first name."), StringLength(100), Display(Name = "First name")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Please enter your last name."), StringLength(100), Display(Name = "Last name")]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "Please enter your email address."), EmailAddress(ErrorMessage = "Please enter a valid email address."), StringLength(256)]
    public string Email { get; set; } = "";

    [Display(Name = "Phone number"), Phone(ErrorMessage = "Please enter a valid phone number.")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Please choose a password."), MinLength(8, ErrorMessage = "Your password must be at least 8 characters."), DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Please confirm your password."), DataType(DataType.Password), Display(Name = "Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    public string ConfirmPassword { get; set; } = "";

    [Required(ErrorMessage = "Please choose how you will use RaceDay.")]
    public string Role { get; set; } = "Participant";

    [Display(Name = "Organiser code (if you were given one)")]
    public string? OrganiserCode { get; set; }
}

// ---------- Profile ----------

// Data and validation rules for the profile page.
public class ProfileViewModel
{
    [Required(ErrorMessage = "Please enter your first name."), StringLength(100), Display(Name = "First name")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Please enter your last name."), StringLength(100), Display(Name = "Last name")]
    public string LastName { get; set; } = "";

    [Display(Name = "Phone number"), Phone(ErrorMessage = "Please enter a valid phone number.")]
    public string? PhoneNumber { get; set; }

    // Display-only: not posted back from the form, so they must be nullable.
    public string? Email { get; set; }
    public string? Role { get; set; }
    public string? PictureUrl { get; set; }
}

// ---------- Participant pages ----------

// Data and validation rules for the event browse page.
public class EventBrowseViewModel
{
    public List<EventInfo> Events { get; set; } = new();
    public string? Search { get; set; }
    public string? Type { get; set; }
    public string? Distance { get; set; }
    public int TotalUpcoming { get; set; }
}

// Data and validation rules for the event details page.
public class EventDetailsViewModel
{
    public EventInfo Event { get; set; } = null!;
    public List<CategoryInfo> Categories { get; set; } = new();
    public EnrolmentInfo? MyEnrolment { get; set; }

    public bool IsPast => Event.EventDate < DateTime.UtcNow;
}

// ---------- Organiser pages ----------

// Data and validation rules for the event summary page.
public class EventSummary
{
    public EventInfo Event { get; set; } = null!;
    public int Total { get; set; }
    public int Pending { get; set; }
    public int Confirmed { get; set; }
}

// Data and validation rules for the dashboard page.
public class DashboardViewModel
{
    public string OrganiserName { get; set; } = "";
    public List<EventSummary> Events { get; set; } = new();

    public int TotalEnrolments => Events.Sum(e => e.Total);
    public int TotalPending => Events.Sum(e => e.Pending);
    public List<EventSummary> Upcoming => Events
        .Where(e => e.Event.EventDate >= DateTime.UtcNow)
        .OrderBy(e => e.Event.EventDate).Take(5).ToList();
}

// Data and validation rules for the event form page.
public class EventFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Please enter the event name."), StringLength(150)]
    public string Name { get; set; } = "";

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please choose the event date and time."), DataType(DataType.DateTime), Display(Name = "Date and time")]
    public DateTime EventDate { get; set; }

    [Required(ErrorMessage = "Please enter the location."), StringLength(200)]
    public string Location { get; set; } = "";

    [Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km."), Display(Name = "Distance (km)")]
    public decimal DistanceKm { get; set; } = 10;

    [Required(ErrorMessage = "Please choose an event type."), Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";

    [Display(Name = "Banner image")]
    public IFormFile? Banner { get; set; }

    public string? ExistingBannerUrl { get; set; }

    public bool IsEdit => Id > 0;
}

// Data and validation rules for the categories page.
public class CategoriesViewModel
{
    public EventInfo Event { get; set; } = null!;
    public List<CategoryInfo> Categories { get; set; } = new();
    public CategoryFormViewModel Form { get; set; } = new();
}

// Data and validation rules for the category form page.
public class CategoryFormViewModel
{
    [Required(ErrorMessage = "Please enter a category name."), StringLength(100), Display(Name = "Category name")]
    public string Name { get; set; } = "";

    [StringLength(500)]
    public string? Description { get; set; }
}

// Data and validation rules for the enrolments page.
public class EnrolmentsViewModel
{
    public EventInfo Event { get; set; } = null!;
    public List<EnrolmentInfo> Enrolments { get; set; } = new();
}

// Data and validation rules for the result row page.
public class ResultRow
{
    public EnrolmentInfo Enrolment { get; set; } = null!;
    public ResultInfo? Result { get; set; }
}

// Data and validation rules for the results page.
public class ResultsViewModel
{
    public EventInfo Event { get; set; } = null!;
    public List<ResultRow> Rows { get; set; } = new();
}

// Data and validation rules for the result form page.
public class ResultFormViewModel
{
    public int EventId { get; set; }
    public int EnrolmentId { get; set; }
    public int? ResultId { get; set; }

    [Required(ErrorMessage = "Enter the finish time as hh:mm:ss."),
     RegularExpression(@"^\d{1,2}:[0-5]\d:[0-5]\d$", ErrorMessage = "Enter the finish time as hh:mm:ss, for example 00:52:30.")]
    public string FinishTime { get; set; } = "";

    [Range(1, 100000, ErrorMessage = "Position must be a whole number of 1 or more.")]
    public int Position { get; set; }
}
