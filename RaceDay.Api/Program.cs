using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Storage;

var builder = WebApplication.CreateBuilder(args);

// ---------- Services (everything the API needs) ----------

// SQL Server database. The connection string is in appsettings.json.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Hashes passwords so they are never stored as plain text.
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();

// Uploads banner and profile images to Azure Blob Storage.
builder.Services.AddSingleton<IBlobStorage, AzureBlobStorage>();

// Session login: the API keeps the logged-in user in the session and sends a cookie to the browser.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(7);      // "remember me" length
    options.Cookie.MaxAge = TimeSpan.FromDays(7);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Our own login scheme reads the user from the session (see SessionAuthHandler).
builder.Services.AddAuthentication(SessionAuth.Scheme)
    .AddScheme<AuthenticationSchemeOptions, SessionAuthHandler>(SessionAuth.Scheme, null);
builder.Services.AddAuthorization();

// Controllers. Enums are sent as words ("Run") instead of numbers (0).
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Swagger: the interactive page that documents and tests the API.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "API for the RaceDay event management system. " +
                      "To try the endpoints, call POST /api/auth/login first. " +
                      "Swagger then keeps the session cookie for the other calls. " +
                      "Demo organiser: thandi.organiser@raceday.co.za, demo participant: " +
                      "sipho.participant@raceday.co.za, password: Password123!"
    });

    // Show the /// comments (summaries, parameter descriptions and examples) on the Swagger page.
    string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);

    // Pre-fill the request body boxes with example JSON (see Swagger/RequestExamples.cs).
    options.SchemaFilter<RaceDay.Api.Swagger.RequestExamples>();

    // Finish times are sent as text such as "00:52:30".
    options.MapType<TimeSpan>(() => new OpenApiSchema
    {
        Type = "string",
        Example = new OpenApiString("00:52:30")
    });
});

var app = builder.Build();

// ---------- Start-up work ----------

// Create the database when the API starts (and add demo data if enabled).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    db.Database.EnsureCreated();

    // Add any columns an older database is missing (for example ProfilePictureUrl and BannerUrl).
    DbPatcher.AddMissingColumns(db);

    if (app.Configuration.GetValue<bool>("SeedDemoData"))
    {
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();
        DbSeeder.Seed(db, hasher);
    }
}

// ---------- Request pipeline (the order matters) ----------

app.UseSwagger();
app.UseSwaggerUI();

app.UseSession();          // 1. read the session cookie
app.UseAuthentication();   // 2. find out who the user is
app.UseAuthorization();    // 3. check they are allowed to call the endpoint

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers();

app.Run();

// Lets the test project start the API in memory.
public partial class Program { }
