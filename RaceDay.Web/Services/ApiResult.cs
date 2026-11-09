using System.Net;

namespace RaceDay.Web.Services;

/// <summary>What came back from the API: the data, or a friendly error message.</summary>
public class ApiResult<T>
{
    // true when the API answered with a success code.
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string Error { get; init; } = "";
    public HttpStatusCode StatusCode { get; init; }

    // true when the API says the user is no longer logged in.
    public bool Unauthorized => StatusCode == HttpStatusCode.Unauthorized;
}
