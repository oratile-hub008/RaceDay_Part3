using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models;
using RaceDay.Web.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Profile page: change my name, phone number and picture.</summary>
[SessionAuthorize]
public class ProfileController : AppController
{
    public ProfileController(ApiService api) : base(api) { }

    // Shows my profile.
    public async Task<IActionResult> Index()
    {
        var result = await Api.GetAsync<UserInfo>("api/users/me");
        if (!result.Success) return ApiFail(result);

        return View(ToModel(result.Data!));
    }

    // Saves my name and phone number.
    [HttpPost]
    public async Task<IActionResult> Update(ProfileViewModel model)
    {
        // Email, role and picture are display-only, so only the editable fields must be valid.
        if (!ModelState.IsValid)
        {
            var current = await Api.GetAsync<UserInfo>("api/users/me");
            if (!current.Success) return ApiFail(current);

            model.Email = current.Data!.Email;
            model.Role = current.Data.Role;
            model.PictureUrl = current.Data.ProfilePictureUrl;
            return View("Index", model);
        }

        var result = await Api.PutAsync<UserInfo>("api/users/me", new
        {
            firstName = model.FirstName,
            lastName = model.LastName,
            phoneNumber = model.PhoneNumber
        });
        if (!result.Success) return RedirectWithError(result, "Index");

        HttpContext.Session.SetString(SessionKeys.Name, $"{result.Data!.FirstName} {result.Data.LastName}");
        TempData["Success"] = "Your profile has been updated.";
        return RedirectToAction("Index");
    }

    // Uploads a new profile picture to Azure through the API.
    [HttpPost]
    public async Task<IActionResult> UploadPicture(IFormFile? picture)
    {
        if (picture == null || picture.Length == 0)
        {
            TempData["Error"] = "Please choose an image to upload.";
            return RedirectToAction("Index");
        }

        var result = await Api.UploadAsync<UserInfo>("api/users/me/picture", picture);
        if (!result.Success) return RedirectWithError(result, "Index");

        HttpContext.Session.SetString(SessionKeys.Picture, result.Data!.ProfilePictureUrl ?? "");
        TempData["Success"] = "Your profile picture has been updated.";
        return RedirectToAction("Index");
    }

    private static ProfileViewModel ToModel(UserInfo user) => new()
    {
        FirstName = user.FirstName,
        LastName = user.LastName,
        PhoneNumber = user.PhoneNumber,
        Email = user.Email,
        Role = user.Role,
        PictureUrl = user.ProfilePictureUrl
    };
}
