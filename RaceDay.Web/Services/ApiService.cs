using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Services;

/// <summary>
/// The only class that talks to the Part 2 API. It forwards the user's API session cookie
/// (stored in the MVC session) and turns every failure into a friendly message.
/// </summary>
public class ApiService
{
    // The name of the cookie the API uses for its session.
    private const string ApiSessionCookieName = ".AspNetCore.Session=";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient http;
    private readonly IHttpContextAccessor accessor;

    public ApiService(HttpClient http, IHttpContextAccessor accessor)
    {
        this.http = http;
        this.accessor = accessor;
    }

    // The web app's own session (it stores the API cookie for the logged-in user).
    private ISession Session => accessor.HttpContext!.Session;

    /// <summary>GET: reads data from the API.</summary>
    public Task<ApiResult<T>> GetAsync<T>(string url) =>
        SendAsync<T>(HttpMethod.Get, url, null);

    /// <summary>POST: creates something through the API.</summary>
    public Task<ApiResult<T>> PostAsync<T>(string url, object? body = null) =>
        SendAsync<T>(HttpMethod.Post, url, body == null ? null : JsonContent.Create(body, options: Json));

    /// <summary>PUT: changes something through the API.</summary>
    public Task<ApiResult<T>> PutAsync<T>(string url, object body) =>
        SendAsync<T>(HttpMethod.Put, url, JsonContent.Create(body, options: Json));

    /// <summary>DELETE: removes something through the API.</summary>
    public Task<ApiResult<object>> DeleteAsync(string url) =>
        SendAsync<object>(HttpMethod.Delete, url, null);

    /// <summary>Sends an image to the API (the API stores it in Azure Blob Storage).</summary>
    public async Task<ApiResult<T>> UploadAsync<T>(string url, IFormFile file)
    {
        using var content = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", file.FileName);

        return await SendAsync<T>(HttpMethod.Post, url, content);
    }

    /// <summary>Logs in through the API and keeps the API's session cookie for later calls.</summary>
    public async Task<ApiResult<UserInfo>> LoginAsync(string email, string password, bool rememberMe)
    {
        // Always start a brand-new API session when someone logs in.
        Session.Remove(SessionKeys.ApiCookie);

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/login")
        {
            Content = JsonContent.Create(new { email, password, rememberMe }, options: Json)
        };

        try
        {
            using var response = await http.SendAsync(request);

            if (response.IsSuccessStatusCode && response.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                string? sessionCookie = cookies.FirstOrDefault(c => c.StartsWith(ApiSessionCookieName));
                if (sessionCookie != null)
                    Session.SetString(SessionKeys.ApiCookie, sessionCookie.Split(';')[0]);
            }

            return await ReadAsync<UserInfo>(response);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return Fail<UserInfo>(HttpStatusCode.ServiceUnavailable);
        }
    }

    // Every call goes through here: add the user's cookie, send, and read the answer.
    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, HttpContent? content)
    {
        using var request = new HttpRequestMessage(method, url) { Content = content };

        string? cookie = Session.GetString(SessionKeys.ApiCookie);
        if (!string.IsNullOrEmpty(cookie))
            request.Headers.TryAddWithoutValidation("Cookie", cookie);

        try
        {
            using var response = await http.SendAsync(request);
            return await ReadAsync<T>(response);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return Fail<T>(HttpStatusCode.ServiceUnavailable);
        }
    }

    // Turns the HTTP response into an ApiResult (data on success, a friendly message on failure).
    private static async Task<ApiResult<T>> ReadAsync<T>(HttpResponseMessage response)
    {
        string body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return Fail<T>(response.StatusCode, ExtractError(response.StatusCode, body));

        T? data = default;
        if (!string.IsNullOrWhiteSpace(body))
        {
            try { data = JsonSerializer.Deserialize<T>(body, Json); }
            catch (JsonException) { return Fail<T>(HttpStatusCode.BadGateway); }
        }

        return new ApiResult<T> { Success = true, Data = data, StatusCode = response.StatusCode };
    }

    // Builds a failed ApiResult.
    private static ApiResult<T> Fail<T>(HttpStatusCode status, string? message = null) =>
        new() { Success = false, StatusCode = status, Error = message ?? DefaultMessage(status) };

    /// <summary>Pulls a readable message out of the API's error JSON; never shows raw errors.</summary>
    private static string ExtractError(HttpStatusCode status, string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String)
                    return error.GetString()!;

                if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    var messages = errors.EnumerateObject()
                        .Where(p => p.Value.ValueKind == JsonValueKind.Array)
                        .SelectMany(p => p.Value.EnumerateArray())
                        .Select(m => m.GetString() ?? "")
                        .Where(m => m.Length > 0);

                    string joined = string.Join(" ", messages);
                    if (joined.Length > 0) return joined;
                }
            }
        }
        catch (JsonException) { }

        return DefaultMessage(status);
    }

    // Plain-English messages for the common status codes.
    private static string DefaultMessage(HttpStatusCode status) => (int)status switch
    {
        400 => "Some of the information you entered is not valid. Please check it and try again.",
        401 => "Please log in again.",
        403 => "You do not have permission to do that.",
        404 => "We could not find what you were looking for.",
        409 => "That could not be done because it conflicts with existing information.",
        503 => "RaceDay cannot be reached right now. Please try again in a moment.",
        >= 500 => "Something went wrong on our side. Please try again.",
        _ => "Something went wrong. Please try again."
    };
}
