using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Login, register and logout pages.</summary>
public class AccountController : AppController
{
    public AccountController(ApiService api) : base(api) { }

    // Shows the login form.
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (CurrentUserId != 0) return RedirectToAction("Index", "Home");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    // Checks the login with the API, then starts the web session.
    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await Api.LoginAsync(model.Email, model.Password, model.RememberMe);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Error);
            return View(model);
        }

        StartSession(result.Data!);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);

        return RedirectToAction("Index", "Home");
    }

    // Shows the registration form.
    [HttpGet]
    public IActionResult Register()
    {
        if (CurrentUserId != 0) return RedirectToAction("Index", "Home");
        return View(new RegisterViewModel());
    }

    // Creates the account through the API and logs the new user in.
    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var created = await Api.PostAsync<UserInfo>("api/auth/register", new
        {
            firstName = model.FirstName,
            lastName = model.LastName,
            email = model.Email,
            password = model.Password,
            confirmPassword = model.ConfirmPassword,
            role = model.Role,
            phoneNumber = model.PhoneNumber,
            organiserCode = model.OrganiserCode
        });

        if (!created.Success)
        {
            ModelState.AddModelError("", created.Error);
            return View(model);
        }

        // Log the new user in straight away.
        var login = await Api.LoginAsync(model.Email, model.Password, false);
        if (!login.Success)
        {
            TempData["Success"] = "Your account was created. Please log in.";
            return RedirectToAction("Login");
        }

        StartSession(login.Data!);
        TempData["Success"] = $"Welcome to RaceDay, {model.FirstName}!";
        return RedirectToAction("Index", "Home");
    }

    // Ends the API session and the web session.
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await Api.PostAsync<object>("api/auth/logout");
        HttpContext.Session.Clear();

        TempData["Success"] = "You have been logged out.";
        return RedirectToAction("Login");
    }
}
