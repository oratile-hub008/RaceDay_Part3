using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

// Use one culture everywhere so decimals (10.5) and dates behave the same locally and in Docker.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    // Every POST needs an anti-forgery token, and protected pages are never cached by the browser.
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    options.Filters.Add(new ResponseCacheAttribute { NoStore = true, Location = ResponseCacheLocation.None });
});

// Session keeps the logged-in user, their role and the API session cookie.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(7);
    options.Cookie.Name = ".RaceDay.Web.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();

// The only way this app gets data: HTTP calls to the Part 2 API.
builder.Services.AddHttpClient<ApiService>(client =>
{
    string baseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5100/";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    // UseCookies = false so users never share cookies; ApiService adds each user's own cookie.
    var handler = new HttpClientHandler { UseCookies = false };
    if (builder.Configuration.GetValue<bool>("ApiSettings:AcceptUntrustedCertificate"))
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    return handler;
});

var app = builder.Build();

// Friendly error pages instead of raw exceptions.
app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithReExecute("/Home/StatusPage", "?code={0}");

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
